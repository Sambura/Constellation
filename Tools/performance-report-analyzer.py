# ALERT: ALL of these imports are EXTREMELY important, they are a BIG DEAL

import os
import json
import matplotlib
from matplotlib.backends.backend_tkagg import FigureCanvasTkAgg
import matplotlib.pyplot as plt
import tkinter as tk
from tkinter import ttk
import numpy as np
import math
import argparse
import sys
import pathlib
from pathlib import Path
import re
from copy import deepcopy
import itertools
import threading
from concurrent.futures import ThreadPoolExecutor
from scipy.stats import gaussian_kde
import inspect
from time import perf_counter

def is_integer(s):
    """can the string `s` be converted to int?""" 
    try: int(s); return True
    except ValueError: return False

def smooth_array(arr, window_size=3, mode='postpad'):
    window_size = min(window_size, len(arr))
    padding_size = window_size // 2
    
    if mode == 'prepad':
        arr = np.pad(arr, (padding_size, padding_size - 1 + window_size % 2), mode='edge')
    arr = np.convolve(arr, np.ones(window_size) / window_size, mode='valid')
    if mode == 'postpad':
        arr = np.pad(arr, (padding_size, padding_size - 1 + window_size % 2), mode='edge')
    return arr

def convert_to_framings(data):
    cum_data = np.cumsum(data)
    return [np.sum(np.logical_and(cum_data < i + 1, cum_data >= i )) for i in range(int(math.ceil(cum_data[-1])))]

def is_child(child_path, parent_path):
    try:
        child_path.relative_to(parent_path)
        return True
    except ValueError:
        return False

def snake_case_to_readable(snake_case):
    return ' '.join(x.capitalize() for x in snake_case.split('_'))

class ReportGroupData:
    options: list[str]
    variable: tk.StringVar
    dropdown: ttk.OptionMenu
    frame: tk.Frame

    def __init__(self, options, var, frame, dropdown, vis_var, vis_dropdown):
        self.options = options
        self.variable = var
        self.frame = frame
        self.dropdown = dropdown
        self.vis_var = vis_var
        self.vis_dropdown = vis_dropdown

class ReportData:
    def __init__(self, timings, filename, json_data):
        self.timings = timings
        self.filename = filename
        self.basename = Path(filename).stem if filename is not None else None
        self.json_data = json_data

    def _parse_file(self):
        with open(self.file_path, 'r') as file:
            self.json_data = json.load(file)

        self.filename = os.path.basename(self.file_path)
        self.timings = 1000 * np.array(self.json_data['Timings'].split(','), dtype=float)
        self.basename = Path(self.filename).stem
        self.fullscreen_mode = self.json_data['FullscreenMode']
        self.system_name = self.json_data['DeviceModel']
        self.operating_system = self.json_data['OperatingSystem']
        self.program_version = self.json_data['ConstellationVersion']
        self.display_resolution = self.json_data['DisplayResolution']

    def parse_file(self, thread_pool=None):
        if thread_pool is not None:
            thread_pool.submit(self._parse_file)
        else:
            self._parse_file()

    def from_file(path, thread_pool=None, do_not_parse=False):
        report = ReportData(None, None, None)
        report.file_path = path

        if not do_not_parse:
            report.parse_file(thread_pool)

        return report

class ReportDataStore:
    def __init__(self, source_directory=None, filename_regex=None, structure_only=False, parse_workers=4):
        # only one of these 2 is used at a time
        self.data = {}
        self.flat_data = []

        self.groups: list[list[str]] = []
        self.depth = 0
        self.filename_regex = filename_regex
        self.source_dirs = []
        self.max_parse_workers = parse_workers
        self.structure_only = structure_only
        self.thread_pool = None
        
        if source_directory is not None:
            self.add_from_directory(source_directory)

    def _add_data_to_store(self, data, destination, group_chain):
        key, *rest = group_chain
        if rest == []:
            destination[key] = data
        else:
            if key not in destination: destination[key] = {}
            self._add_data_to_store(data, destination[key], rest)
    
    def _register_group_values(self, group_chain):
        self.depth = max(self.depth, len(group_chain))
        for i in range(len(group_chain)):
            if len(self.groups) <= i: self.groups.append([])
            self.groups[i] = list(np.unique(self.groups[i] + [group_chain[i]]))

    def _make_thread_pool(self):
        if self.thread_pool is None:
            self.thread_pool = ThreadPoolExecutor(max_workers=self.max_parse_workers)

    def is_flat(self): return self.flat_data != []

    def add(self, group_chain: list[str], data: ReportData):
        if group_chain == []:
            self.flat_data.append(data)
            return

        self._register_group_values(group_chain)
        data.group_chain = group_chain
        self._add_data_to_store(data, self.data, group_chain)

    def get_report(self, group_chain):
        data = self.data
        try:
            for i in range(len(group_chain)):
                data = data[group_chain[i]]
        except KeyError:
            return None

        return data

    def add_file(self, path, *parents):
        # if filename has a report index (e.g. benchmark-3-report.json), extract this index
        filename = os.path.basename(path)
        match = re.search(self.filename_regex, filename)
        if match:
            base_name = match.group(1)
            index = str(int(match.group(2)))
            parents = list(parents) + [base_name, index]
        else:
            parents = list(parents) + [filename]

        self.add(parents, ReportData.from_file(path, self.thread_pool, do_not_parse=self.structure_only))

    def add_from_directory(self, directory: str, *parents: list[str]):
        "parents - an ordered list of group names that this directory belongs to"
        self._make_thread_pool()
        directory = directory if isinstance(directory, Path) else Path(directory)
        dirs = [directory / item.name for item in directory.iterdir() if item.is_dir()]
        files = [directory / item.name for item in directory.iterdir() if item.is_file() and item.suffix == '.json']
        if not np.any([is_child(directory, x) for x in self.source_dirs]):
            self.source_dirs.append(directory)

        if len(dirs) > 0 and len(files) > 0:
            print(f'Warning: directory {directory} contains both json files and directories')

        parents = list(parents) + [directory.name]
        if len(files) > 0:
            for file in files:
                self.add_file(file, *parents)

        for subdir in dirs:
            self.add_from_directory(subdir, *parents)

    def iterate(self):
        "Go through all the stored reports. Yields tuple (group_chain, report_data)"
        group_chains = itertools.product(*self.groups)
        for group_chain in group_chains:
            report = self.get_report(group_chain)
            if report is None: continue
            yield group_chain, report

    def build_subtree(self, selected_values: list[str], compress=True):
        """Prunes data tree based on selected group values. selected_values should have the same length
        as the number of groups in the tree. Each selected value should either be one of the group values
        on the respective depth to select only reports with that value, or None to select the whole group.
        Specify compress=True to remove groups with only 1 value from the resulting tree. Example tree:
            [reports] -> [old-version] -> [run-1]
                    \                 \-> [run-2]
                     \-> [new-version] -> [run-1]
                                      \-> [run-2]
        
        Running with selected_values = [None, None, None] and compress=False will produce the same tree
        Running with selected_values = [None, None, None] and compress=True will remove `reports` group:
            [old-version] -> [run-1]
                         \-> [run-2]
            [new-version] -> [run-1]
                         \-> [run-2]


        Running with selected_values = [None, None, 'run-1'] and compress=False will produce the following:
            [reports] -> [old-version] -> [run-1]
                     \-> [new-version] -> [run-1]

        Running with selected_values = [None, None, 'run-1'] and compress=True:
            [old-version] (this is reports/old-version/run-1)
            [new-version] (this is reports/new-version/run-1)

        Returns a new ReportDataStore
        """
        subtree = ReportDataStore()
        select_groups = [True] * len(self.groups)
        if compress:
            select_groups = [len(x) > 1 and y is None for x, y in zip(self.groups, selected_values)]

        for group_chain, report in self.iterate():
            # filtering based on selected values
            if np.any([y is not None and x != y for x, y in zip(group_chain, selected_values)]): continue

            selected_chain = [x for x, y in zip(group_chain, select_groups) if y]
            subtree.add(selected_chain, report)

        return subtree

    def make_flat_subtree(self, preserve_group_depth: int):
        """Flattens the data tree, preserving grouping on a specified depth. For example the following tree:
            [reports] -> [old-version] -> [run-1]
                    \                 \-> [run-2]
                     \-> [new-version] -> [run-1]
                                      \-> [run-2]
                                    
        - can be flattened on depth 1 - grouping data by version. Resulting tree is:
            [old-version] -> [reports/run-1]
                         \-> [reports/run-2]
            [new-version] -> [reports/run-1]
                         \-> [reports/run-2]
        
        Alternatively, you can group data on depth 2:
            [run-1] -> [reports/old-version]
                   \-> [reports/new-version]
            [run-2] -> [reports/old-version]
                   \-> [reports/new-version]

        Currently only supports grouping on a single depth. Returns a new ReportDataStore
        """
        subtree = ReportDataStore()

        for group_chain, report in self.iterate():
            group_value = group_chain[preserve_group_depth]
            new_key = '/'.join(group_chain[:preserve_group_depth] + group_chain[preserve_group_depth + 1:])
            subtree.add([group_value, new_key], report)

        return subtree

    def transpose_groups(self, transpose_map: list[int]):
        """Produces a new data store with groups swapped according to transposition map
        value at index 3 in transpose_map corresponds to target group index at depth 3
        """
        new_tree = ReportDataStore()
        if len(np.unique(transpose_map)) != len(transpose_map): 
            raise ValueError('Invalid transpose map')

        for group_chain, report in self.iterate():
            group_chain = [group_chain[x] for x in transpose_map]
            new_tree.add(group_chain, report)

        return new_tree

    def prepend_group(self, group_value):
        "Make a new tree that has one more group with a single value. This group becomes the first one"
        new_tree = ReportDataStore()

        for group_chain, report in self.iterate():
            new_tree.add([group_value, *group_chain], report)

        return new_tree

    def wait_for_completion(self):
        if self.thread_pool is None: return
        self.thread_pool.shutdown(wait=True)
        self.thread_pool = None

    def load_contents(self):
        self.structure_only = False
        self._make_thread_pool()
        for _, report in self.iterate():
            report.parse_file(self.thread_pool)

        return self

class VariableStore:
    """
    Attributes:
        variables: dict - contains var_name: (variable, widget, callback)
    """

    def __init__(self, ui_frame):
        self.variables = { }
        self.frame = ui_frame
        self.callbacks = []
    
    def register_variable(self, name, variable, label=None):
        if name in self.variables: raise ValueError('Variable already exists')
        if label is None: label = snake_case_to_readable(name)

        def _callback():
            self._on_value_changed(name)

        callback = _callback
        if isinstance(variable, tk.BooleanVar):
            widget = tk.Checkbutton(self.frame, text=label, variable=variable, command=callback)
        elif isinstance(variable, tk.StringVar):
            widget = ttk.Entry(self.frame, width=6, textvariable=variable)
            widget.bind('<Return>', lambda event: callback())
        elif isinstance(variable, tk.IntVar):
            default_value = variable.get()
            intermediate = tk.StringVar(value=str(default_value))
            widget = ttk.Entry(self.frame, width=6, textvariable=intermediate)
            def _parse_int_callback():
                if not is_integer(intermediate.get()): return
                variable.set(int(intermediate.get()))
                _callback()

            callback = _parse_int_callback
            widget.bind('<Return>', lambda event: callback())
        elif isinstance(variable, tk.DoubleVar):
            default_value = variable.get()
            intermediate = tk.StringVar(value=str(default_value))
            widget = ttk.Entry(self.frame, width=6, textvariable=intermediate)
            def _parse_float_callback():
                try:
                    variable.set(float(intermediate.get()))
                except ValueError: return
                _callback()

            callback = _parse_float_callback
            widget.bind('<Return>', lambda event: callback())
        else:
            raise NotImplementedError(f'No support for {type(variable)} yet')
        
        widget.pack(side=tk.LEFT, fill=tk.X, expand=True)

        self.variables[name] = (variable, widget, callback)

    def register_callback(self, callback):
        "Callback either accepts a variable name or no arguments"
        self.callbacks.append(callback)

    def _on_value_changed(self, name):
        for callback in self.callbacks:
            params = (name,) if len(inspect.signature(callback).parameters) == 1 else ()
            callback(*params)

    def __getitem__(self, name):
        return self.variables[name][0].get()

class ReportAnalyzer:
    report_groups: list[ReportGroupData] = []
    canvas: FigureCanvasTkAgg = None
    reports_store: ReportDataStore = None
    current_plotted_data: dict[...] = None

    ###
    ### Dynamic UI stuff
    def reset_report_groups(self):
        for report_group in self.report_groups:
            report_group.frame.pack_forget()

        self.report_groups = []

    def enumerate_report_groups(self, groups: list[dict[...]]):
        if len(groups) == 0: return

        group_values, subgroups = [], []
        for group in groups:
            group_values += list(group.keys())
            subgroups += [x for x in group.values() if not isinstance(x, ReportData)]
        
        self.add_report_group(list(np.unique(group_values)))
        self.enumerate_report_groups(subgroups)

    def add_report_group(self, options):
        default_value = '-' if len(options) != 1 else options[0]
        extended_options = options if len(options) == 1 else options + ['-']
        variable = tk.StringVar(value='-')
        selector_var = tk.StringVar(value='Auto')

        frame = tk.Frame(self.top_frame)
        frame.pack(side=tk.LEFT, fill=tk.X, expand=True)

        dropdown = ttk.OptionMenu(frame, variable, default_value, *extended_options, command=lambda x: self.update_plots())
        dropdown.pack(side=tk.BOTTOM, fill=tk.BOTH, expand=True)
        vis_selector = ttk.OptionMenu(frame, selector_var, 'Auto', *['Auto', 'Group', 'Merge axis', 'Plot each'], command=lambda x: self.update_plots())
        vis_selector.pack(side=tk.BOTTOM, fill=tk.BOTH, expand=True)
        
        self.report_groups.append(ReportGroupData(options, variable, frame, dropdown, selector_var, vis_selector))

    ###
    ### Plotting utils
    def update_plots(self):
        if self.reports_store is None: return
        self.progress.start()
        self.reports_store.wait_for_completion()

        # plot_type = self.plot_mode_string_var.get()
        self.current_plotted_data = None
        def do_compute():
            self.current_plotted_data = self.prepare_composite_data()
            self.progress.stop()

        # def do_plot():
        #     if plot_type == 'composite':
        #         self.plot_composite()
        #     elif plot_type == 'FPS':
        #         self.plot_fps_simple()
        #     else:
        #         self.plot_timings_simple()

        worker_thread = threading.Thread(target=do_compute)
        worker_thread.start()

    # if I don't recreate the canvas, it doesn't draw it fullscreen for some reason
    # presumably this will not be an issue if you don't need to change figure, but i didn't test
    def update_canvas(self, new_fig, recreate=False):
        if self.canvas is not None:
            plt.close(self.canvas.figure)
            self.canvas.get_tk_widget().pack_forget()
        self.canvas = FigureCanvasTkAgg(new_fig, master=self.root)
        self.canvas.draw()
        self.canvas.get_tk_widget().pack(fill=tk.BOTH, expand=True)

    def subplots2d(self, n, m, figsize=None):
        if figsize is None: figsize = (n*6, m*4)
        fig, axs = plt.subplots(n, m, figsize=figsize)
        if not hasattr(axs, '__len__'): axs = np.array([[axs]])
        if not hasattr(axs[0], '__len__'): axs = np.array([axs]).reshape(-1, 1)

        return fig, axs

    def make_subplot_grid(self, subplots_count, figsize=None):
        n = m = math.ceil(math.sqrt(subplots_count))
        if n * (n - 1) >= subplots_count: m -= 1

        fig, axs = self.subplots2d(n, m, figsize=figsize)
        return fig, axs.ravel()

    def get_selected_report_data(self, compress):
        "Returns a subset of data stored in self.reports_store. The data is pruned based on selected report groups"
        selected_values = []
        for report_group in self.report_groups:
            if report_group.variable.get() == '-':
                selected_values.append(None)
            else:
                selected_values.append(report_group.variable.get())
        
        return self.reports_store.build_subtree(selected_values, compress)

    def get_group_plot_tags(self):
        return [x.vis_var.get() for x in self.report_groups]

    def set_status(self, text: str, error: bool=False):
        self.status_label.config(text=text, background='salmon' if error else 'lightgreen')

    def get_and_check_selected_data(self, min_depth=None, max_depth=None, compress=True):
        "Calls get_selected_report_data and checks if it has acceptable depth. Updates status label"
        base_data = self.get_selected_report_data(compress)

        if min_depth is not None and base_data.depth < min_depth:
            self.set_status('Select less', error=True)
            return None
        elif max_depth is not None and base_data.depth > max_depth:
            self.set_status('Select more', error=True)
            return None
        
        self.set_status('Ok')

        return base_data

    def display_plot(self, fig):
        plt.tight_layout()
        self.update_canvas(fig)

    def plot_timings_simple(self):
        base_data = self.get_and_check_selected_data(max_depth=2)
        if base_data is None: return
        plot_fps = self.var_store['plot_fps']

        def setup_axis(ax, title):
            ax.set_title(title)
            ax.set_xlabel("Frame index")
            ax.set_ylabel("FPS" if plot_fps else "Frame duration (ms)")

        def plot_timings(ax, report):
            plot_data = 1000 / report.timings if plot_fps else report.timings 
            ax.plot(plot_data, label=report.basename)

        if base_data.is_flat(): # only one plot needs to be drawn
            fig, axs = self.make_subplot_grid(1)
            setup_axis(axs[0], base_data.flat_data[0].basename)
            plot_timings(axs[0], base_data.flat_data[0])
            axs[0].legend()

        else: # one or more plots in each of the subplots need to be drawn
            base_data = base_data.data
            fig, axs = self.make_subplot_grid(len(base_data))
            for ax, name in zip(axs, base_data):
                setup_axis(ax, name)

                if isinstance(base_data[name], ReportData):
                    plot_timings(ax, base_data[name])
                else:
                    for line_data in base_data[name]:
                        plot_timings(ax, base_data[name][line_data])

                ax.legend()

        self.display_plot(fig)

    def plot_fps_simple(self):
        base_data = self.get_and_check_selected_data(max_depth=2)
        if base_data is None: return

        def setup_axis(ax, title):
            ax.set_title(title)
            ax.set_xlabel("Time (s)")
            ax.set_ylabel("Frame per second")

        def plot_fps(ax, report):
            # fps_ps = 2 # fps per second == histogram bars (bins) per second
            timings = np.cumsum(report.timings)
            stop_time = int(math.ceil(timings[-1]))
            ax.hist(timings, bins=stop_time, range=(0, stop_time), label=report.basename, alpha=0.5)

        if base_data.is_flat(): # only one plot needs to be drawn
            fig, axs = self.make_subplot_grid(1)
            setup_axis(axs[0], base_data.flat_data[0].basename)
            plot_fps(axs[0], base_data.flat_data[0])
            axs[0].legend()
        else:
            base_data = base_data.data
            fig, axs = self.make_subplot_grid(len(base_data))
            for ax, name in zip(axs, base_data):
                setup_axis(ax, name)

                if isinstance(base_data[name], ReportData):
                    plot_fps(ax, base_data[name])
                else:
                    for line_data in base_data[name]:
                        plot_fps(ax, base_data[name][line_data])

                ax.legend()

        self.display_plot(fig)

    def prepare_composite_data(self):
        base_data = self.get_and_check_selected_data(min_depth=2, compress=False)
        if base_data is None: return

        # ['Auto', 'Group', 'Merge axis', 'Plot each']
        def auto_assign_tag(plot_tags, tag, condition=lambda x: True, max_count=None):
            if max_count is None: max_count = float('inf')
            tags = plot_tags[:]
            for i in range(len(tags)):
                if tags.count(tag) >= max_count: break

                if tags[i] != 'Auto' or not condition(i): continue
                tags[i] = tag
            
            return tags

        plot_tags = self.get_group_plot_tags()
        group_value_count = [len(x) for x in base_data.groups]
        # verification
        if plot_tags.count('Plot each') > 1: self.set_status('Multiple `Plot each` tags', error=True); return
        if plot_tags.count('Group') > 1: self.set_status('Multiple `Group` tags', error=True); return
        # replace `Auto` tags
        plot_tags = auto_assign_tag(plot_tags, 'Group', lambda x: group_value_count[x] >= 2, max_count=1)
        plot_tags = auto_assign_tag(plot_tags, 'Merge axis')

        plot_each_index = plot_tags.index('Plot each') if 'Plot each' in plot_tags else -1
        group_plot_index = plot_tags.index('Group') if 'Group' in plot_tags else -1

        transpose_map = []
        if plot_each_index >= 0: transpose_map.append(plot_each_index)
        if group_plot_index >= 0: transpose_map.append(group_plot_index)
        for i in range(len(plot_tags)):
            if i in transpose_map: continue
            transpose_map.append(i)

        # Reorder groups for simplicity
        base_data = base_data.transpose_groups(transpose_map)
        if plot_each_index < 0: # still no each plot tag? make one up
            plot_name = 'Composite data'
            for i in range(len(plot_tags)):
                if len(base_data.groups[i]) == 1:
                    plot_name = base_data.groups[i][0]
            base_data = base_data.prepend_group(plot_name)

        # get parameters
        as_distribution = self.var_store['plot_distribution']
        sort_timings = self.var_store['sort_timings']
        time_axis = self.var_store['time_axis']
        smoothing_window = self.var_store['smoothing_window']
        plot_fps = self.var_store['plot_fps']
        exclude_outliers = self.var_store['exclude_outliers']
        base_exclusion_threshold = self.var_store['exclusion_threshold']

        # prepare initial dataset, work from there
        merged_data = { }
        plot_names = base_data.groups[0] # group values associated with `Plot each` tag (which is always first)
        for plot_name in plot_names:
            composite_data = { } # { group_value: (timings, separators), ... }
            
            # get branch of the tree with only reports for `plot_name`, and flatten with respect to grouping tag
            data = base_data.build_subtree([plot_name] + [None] * (base_data.depth - 1), compress=False)
            data = data.make_flat_subtree(1).data

            for group_value, subgroup in data.items(): # primary group
                if len(subgroup) == 0: continue

                for report in subgroup.values(): # flattening all other groups
                    if len(report.timings) == 0: continue

                    if group_value not in composite_data:
                        composite_data[group_value] = []
                    
                    composite_data[group_value].append(report.timings)

            # at this point we have a dict with complete datasets. It's time to filter out the outliers (if applicable)
            if exclude_outliers:
                for group_value, timings in list(composite_data.items()):
                    exclusion_threshold = base_exclusion_threshold
                    timings_flat = np.concatenate(timings)
                    baseline, std = np.mean(timings_flat), np.std(timings_flat)
                    means = np.array([np.mean(x) for x in timings])
                    while True: # in case we try to exclude too much, increase threshold
                        exclude = np.abs(means - baseline) > std * exclusion_threshold
                        if np.sum(exclude) < len(timings) // 2: break
                        exclusion_threshold *= 1.1

                    composite_data[group_value] = [x for i, x in enumerate(timings) if not exclude[i]]
            
            # convert lists of timings to one array + separators:
            for group_value, timings_list in list(composite_data.items()):
                composite_data[group_value] = (np.concatenate(timings_list), np.cumsum([len(x) for x in timings_list]))

            merged_data[plot_name] = composite_data

        if as_distribution: # further processing for distribution plotting
            def compute_density(plot_name, group_value, timings):
                mean, std = np.mean(timings), np.std(timings)
                rng = np.ptp(timings)
                actual_range = (np.min(timings) - rng * 0.01, np.max(timings) + rng * 0.01)
                visible_deviations = 3.3
                visible_range = (mean - visible_deviations * std, mean + visible_deviations * std)
                effective_range = (max(visible_range[0], actual_range[0]), min(visible_range[1], actual_range[1]))

                x_axis = np.linspace(*effective_range, 1000)
                density = gaussian_kde(timings)(x_axis)
                
                return plot_name, group_value, (x_axis, density, mean, std)

            with ThreadPoolExecutor(max_workers=self.max_threads) as pool:
                distribution_data = {}
                futures = []

                for plot_name, comp_data in merged_data.items():
                    distribution_data[plot_name] = { }
                    futures += [
                        pool.submit(compute_density, plot_name, group_value, timings)
                        for group_value, (timings, separators) in comp_data.items()
                    ]

                for future in futures:
                    plot_name, group_value, group = future.result()
                    distribution_data[plot_name][group_value] = group

            return distribution_data

        # "post-processing" based on variables
        composite_data = { }
        for plot_name, data in merged_data.items():
            group = {}
            for group_value, (timings, separators) in data.items():
                x_axis = list(range(len(timings)))
                use_time_axis = time_axis and not sort_timings
                if use_time_axis:
                    x_axis = np.cumsum(timings) / 1000
                if sort_timings: # so that all plots in a group share x axis :)
                    x_axis = np.linspace(0, 1, len(timings))
                
                if sort_timings:
                    timings = np.sort(timings)
                if plot_fps:
                    timings = 1000 / timings

                smoothed = smooth_array(timings, window_size=smoothing_window)

                group[group_value] = (x_axis, timings, smoothed, separators)

            composite_data[plot_name] = group

        return composite_data

    def plot_composite(self, data):
        fig, axs = self.make_subplot_grid(len(data))

        sort_timings = self.var_store['sort_timings']
        only_smoothed = self.var_store['hide_raw']
        time_axis = self.var_store['time_axis']
        as_distribution = self.var_store['plot_distribution']
        plot_fps = self.var_store['plot_fps']
        show_separators = self.var_store['show_separators']
        use_time_axis = time_axis and not sort_timings

        def plot_group(ax, data, title):
            ax.set_title(title)

            if as_distribution:
                self.draw_density_group(ax, data)
                return

            total_data = []
            for plot_name, (x_axis, timings, smoothed, separators) in data.items():
                color = None

                if not only_smoothed or sort_timings:
                    color = ax.plot(x_axis, timings, label=plot_name, alpha=1 if sort_timings else 0.35, lw=1)[0].get_color()
                    total_data = np.concatenate((total_data, timings))

                if not sort_timings:
                    color = ax.plot(x_axis, smoothed, label=plot_name, color=color)[0].get_color()
                    total_data = np.concatenate((total_data, smoothed))
                mean = np.mean(timings)
                ax.axhline(mean, color=color, linestyle='--', lw=3, alpha=0.6)
                ax.text(0, mean, f'{mean:0.2f}')

                if not sort_timings and show_separators:
                    for separator in separators[:-1]:
                        if separator <= 0: continue # shouldn't really happen but who knows
                        x = x_axis[separator - 1]
                        ax.axvline(x, color='k', lw=1, linestyle=':', alpha=0.5)

            total_data = np.sort(total_data)
            top_limit = total_data[int(len(total_data) * 0.999 - 0.999)] * 1.01
            bottom_limit = total_data[int(len(total_data) * 0.001)] / 1.01
            
            ax.set_ylim(bottom_limit, top_limit)
            x_label = 'Time (s)' if use_time_axis else 'Frame index'
            if sort_timings: x_label = 'Frames'
            ax.set_xlabel(x_label)
            ax.set_ylabel('FPS' if plot_fps else 'Frame duration (ms)')
            ax.legend(loc='upper left', fontsize='small')

        for i, (plot_name, plot_data) in enumerate(data.items()):
            plot_group(axs[i], plot_data, plot_name)

        self.display_plot(fig)
    
    def draw_density_group(self, ax, data):
        "data : dict of { plot_name: (x_values, y_values, time_mean, time_std) }"
        # total_x, std_lims = [], []
        last_y, step_y = None, None
        for plot_name, (x_axis, density, mean, std) in data.items():
            color = ax.plot(x_axis, density, label=plot_name)[0].get_color()
            ax.fill_between(x_axis, density, color=color, alpha=0.15)
            ax.axvline(mean, color=color, linestyle='--', lw=2, alpha=0.8)
            ax.axvspan(mean - std, mean + std, color=color, alpha=0.2)
            # total_x = np.concatenate((total_x, x_axis))
            
            if last_y is None:
                max_density = np.max(density)
                last_y = max_density * 0.95
                step_y = max_density * 0.1
            else: 
                last_y -= step_y
            
            ax.text(mean, last_y, f'{mean:0.2f}')
            # std_lims += [mean - 3 * std, mean + 3 * std]

        ax.set_xlabel('Frame duration (ms)')
        ax.set_ylabel('Frequency')
        ax.legend(fontsize='small')
        # ax.set_xlim(max(np.min(total_x), np.min(std_lims)), min(np.max(total_x), np.max(std_lims)))

    def top_menu_file_open(self):
        directory = tk.filedialog.askdirectory(title='Select directory with reports', mustexist=True)
        self.open_reports_directory(directory)

    def open_reports_directory(self, directory):
        if directory is None: return
        new_data = ReportDataStore(directory, self.report_index_regex, structure_only=True)
        if self.reports_store is None:
            self.reports_store = new_data.load_contents()
        elif self.reports_store.source_dirs[0].samefile(new_data.source_dirs[0]):
            tk.messagebox.showinfo("Warning", "This directory was already open. No data was loaded")
            return
        elif self.reports_store.depth == new_data.depth and self.reports_store.groups[1:] == new_data.groups[1:]:
            result = tk.messagebox.askquestion("What do you want to do with new data?",
                                               "The data from this directory is compatible with currently open "
                                               "directory. Do you want to merge the two datasets?",
                                               icon='question', type=tk.messagebox.YESNOCANCEL)
            if result == 'yes': # just casually wasting work done by ReportDataStore() call
                self.reports_store.add_from_directory(directory)
            elif result == 'no':
                self.reports_store = new_data.load_contents()
        else:
            result = tk.messagebox.askquestion("Replace data?",
                                               "Are you sure you want to load this data and replace the currently"
                                               " opened dataset?", icon='question', type=tk.messagebox.YESNO)
            if result == 'no':
                return

            self.reports_store = new_data.load_contents()

        self.reset_report_groups()
        self.enumerate_report_groups([self.reports_store.data])
        self.update_plots()

    def analysis_menu_consistency_report(self):
        oses = []
        devices = []
        fs_modes = []
        versions = []
        resolutions = []

        for _, report in self.reports_store.iterate():
            oses.append(report.operating_system)
            devices.append(report.system_name)
            fs_modes.append(report.fullscreen_mode)
            versions.append(report.program_version)
            res = report.display_resolution
            resolutions.append(f'{res["width"]}x{res["height"]}@{res["refreshRate"]}')

        report = ''
        mismatch_count = 0
        def append_report(name, options, to_string=None):
            nonlocal report, mismatch_count
            unique = np.unique(options)
            if to_string is not None:
                print(type(unique[0]))
                unique = [to_string(x) for x in unique]

            if len(unique) == 1:
                report += f' - {name}: {unique[0]}\n'
            else:
                report += f' - {name}: Inconsistent values:\n'
                report += ', '.join([f'"{x}" ({options.count(x)})' for x in unique])
                report += '\n'
                mismatch_count += 1
        
        append_report('Operating systems', oses)
        append_report('Test devices', devices)
        append_report('Fullscreen modes', fs_modes)
        append_report('Constellation versions', versions)
        append_report('Resolutions', resolutions)

        args = (f"Consistency report: {mismatch_count} mismatches", report)
        if mismatch_count == 0:
            tk.messagebox.showinfo(*args)
        elif mismatch_count == 1:
            tk.messagebox.showwarning(*args)
        else:
            tk.messagebox.showerror(*args)

    def update(self):
        try:
            if self.current_plotted_data is not None:
                self.plot_composite(self.current_plotted_data)
                self.current_plotted_data = None
        finally:
            self.root.after(100, self.update)

    def __init__(self, reports_dir):
        self.reports_dir = reports_dir
        self.report_index_regex = r'^(.*?)-(\d+)-report.json$'
        matplotlib.rcParams['axes.xmargin'] = 0.01
        matplotlib.rcParams['axes.ymargin'] = 0.02
        self.max_threads = 10

    def main(self):
        self.root = tk.Tk()
        self.root.title("Constellation report analyzer")

        # frame for all control widgets
        self.top_frame = tk.Frame(self.root)
        self.top_frame.pack(fill=tk.X)

        self.config_frame = tk.Frame(self.root)
        self.config_frame.pack(fill=tk.X)

        self.var_store = VariableStore(self.config_frame)
        self.var_store.register_variable('smoothing_window', tk.IntVar(value=50))
        self.var_store.register_variable('plot_distribution', tk.BooleanVar(value=False))
        self.var_store.register_variable('sort_timings', tk.BooleanVar(value=False))
        self.var_store.register_variable('plot_fps', tk.BooleanVar(value=False), label='Plot FPS')
        self.var_store.register_variable('show_separators', tk.BooleanVar(value=False))
        self.var_store.register_variable('time_axis', tk.BooleanVar(value=True))
        self.var_store.register_variable('hide_raw', tk.BooleanVar(value=False), label='Hide Raw Data')
        self.var_store.register_variable('exclude_outliers', tk.BooleanVar(value=False), label='Exclude outliers')
        self.var_store.register_variable('exclusion_threshold', tk.DoubleVar(value=0.8))
        self.var_store.register_callback(self.update_plots)

        self.left_top_frame = tk.Frame(self.top_frame)
        self.left_top_frame.pack(side=tk.LEFT, fill=tk.X, expand=True)
        
        self.status_label = ttk.Label(self.left_top_frame)
        self.status_label.pack(side=tk.TOP, fill=tk.BOTH, expand=True)
        self.status_label.config(text='Hello world!')

        self.progress = ttk.Progressbar(self.left_top_frame, mode='indeterminate')
        self.progress.pack(side=tk.TOP, fill=tk.BOTH, expand=True)

        # Create a dropdown menu for selecting plotting mode
        # self.plot_mode_string_var = tk.StringVar()
        # plot_modes = ['Frame timings', 'composite', 'FPS']
        # mode_menu = ttk.OptionMenu(self.top_frame, self.plot_mode_string_var, plot_modes[1], *plot_modes, command=lambda x: self.update_plots())
        # mode_menu.pack(side=tk.RIGHT, fill=tk.X, expand=True)

        # Create menu bar
        menubar = tk.Menu(self.root)

        file_menu = tk.Menu(menubar, tearoff=0)
        file_menu.add_command(label="Open/Add", command=self.top_menu_file_open)
        file_menu.add_separator()
        file_menu.add_command(label="Exit", command=self.root.quit)
        menubar.add_cascade(label="File", menu=file_menu)

        analysis_menu = tk.Menu(menubar, tearoff=0)
        analysis_menu.add_command(label="Consistency report", command=self.analysis_menu_consistency_report)
        menubar.add_cascade(label="Analyze", menu=analysis_menu)
        self.root.config(menu=menubar)

        self.open_reports_directory(self.reports_dir)
        self.root.after(100, self.update)

        # When the window contains plots, program refuses to quit when clicking `close`, so we are forcing it
        self.root.protocol("WM_DELETE_WINDOW", lambda: sys.exit(0)) # TODO : fix?
        self.root.mainloop()

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description='Helper tool to visualize multiple benchmark reports')
    parser.add_argument('--dir', help='Path to (potentially nested) directory with reports')
    args = parser.parse_args()

    app = ReportAnalyzer(args.dir)
    app.main()
