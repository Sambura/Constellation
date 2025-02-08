import os
import json
from matplotlib.backends.backend_tkagg import FigureCanvasTkAgg
import matplotlib.pyplot as plt
import tkinter as tk
from tkinter import ttk
import numpy as np
import math
import argparse
import sys
import pathlib

def is_integer(s):
    """can the string `s` be converted to int?""" 
    try: int(s); return True
    except ValueError: return False

def smooth_array(arr, window_size=3):
    padding_size = window_size // 2
    padded_arr = np.pad(arr, (padding_size, padding_size), mode='edge')
    smoothed_arr = np.convolve(padded_arr, np.ones(window_size) / window_size, mode='valid')
    return smoothed_arr

def convert_to_framings(data):
    cum_data = np.cumsum(data)
    return [np.sum(np.logical_and(cum_data < i + 1, cum_data >= i )) for i in range(int(math.ceil(cum_data[-1])))]

class ReportAnalyzer:
    report_groups: list[list[str]] = []
    report_group_vars: list = []
    dropdown_widgets: list = []

    def update_plots(self):
        plot_params = [x.get() for x in self.report_group_vars]

        if self.mode_var.get() == 'composite':
            self.plot_composite(plot_params)
        elif self.mode_var.get() == 'framings':
            self.plot_groups_framings(plot_params)
        else:
            self.plot_groups(plot_params)

    def add_report_group(self, options):
        print(f'Registering new report group on level {len(self.report_groups)}: {options}')

        variable = tk.StringVar(value='-')
        dropdown = ttk.OptionMenu(self.top_frame, variable, '-', *options, '-', command=lambda x: self.update_plots())
        dropdown.pack(side=tk.LEFT, fill=tk.X, expand=True)
        
        self.report_groups.append(options)
        self.report_group_vars.append(variable)
        self.dropdown_widgets.append(dropdown)

    def draw_only_smoothed(self):
        return self.only_smoothed_var.get()

    def add_data_to_store(self, data, destination, *parents):
        key, *rest = parents
        if len(rest) == 0:
            destination[key] = data
        else:
            if key not in destination: destination[key] = {}
            self.add_data_to_store(data, destination[key], *rest)

    def parse_file(self, path, *parents):
        data = {}

        with open(path, 'r') as file:
            json_data = json.load(file)

        data['timings'] = np.array(json_data['Timings'].split(','), dtype=float)
        data['filename'] = os.path.basename(os.path.basename(path))
        data['json_data'] = json_data
        data['/@/'] = True # hackkk

        self.add_data_to_store(data, self.timing_data, *parents)

    def parse_directory(self, directory, *parents):
        dirs = [directory / item.name for item in directory.iterdir() if item.is_dir()]
        files = [directory / item.name for item in directory.iterdir() if item.is_file() and item.suffix == '.json']

        if len(dirs) > 0 and len(files) > 0:
            print(f'Warning: directory {directory} contains both json files and directories')

        if len(dirs) > len(files):
            for subdir in dirs:
                self.parse_directory(subdir, *parents, directory.name)
        else:
            for file in files:
                self.parse_file(file, *parents, directory.name, file.name)

    def enumerate_groups(self, *group_list):
        if len(group_list) == 0: return

        group_names = []
        sub_list = []
        for group in group_list:
            group_names += list(group.keys())
            sub_list += list(group.values())

        sub_list = [sub for sub in sub_list if '/@/' not in sub]
        group_names = [group_name for group_name in group_names]
        self.add_report_group(list(set(group_names)))
        self.enumerate_groups(*sub_list)

    def parse_data(self, reports_dir):
        """Top level parsing function, accepts top directory path (as a string)"""
        self.timing_data = {}
        root_path = pathlib.Path(reports_dir)
        self.parse_directory(root_path)

    def update_canvas(self, new_fig):
        if self.canvas is not None: self.canvas.get_tk_widget().pack_forget()
        self.canvas = FigureCanvasTkAgg(new_fig, master=self.root)
        self.canvas.draw()
        self.canvas.get_tk_widget().pack(fill=tk.BOTH, expand=True)

    def subplots2d(self, n, m, figsize=None):
        if figsize is None: figsize = (n*6, m*4)
        fig, axs = plt.subplots(n, m, figsize=figsize)
        if not hasattr(axs, '__len__'): axs = np.array([[axs]])
        if not hasattr(axs[0], '__len__'): axs = np.array([axs]).reshape(-1, 1)

        return fig, axs

    def plot_groups(self, plot_params):
        base_data = self.timing_data
        print(f'Plot groups: params {plot_params}')

        while len(plot_params) > 0:
            if plot_params[0] == '-': break
            base_data = base_data[plot_params[0]]
            plot_params = plot_params[1:]

        plot_params = plot_params[1:]
        n = m = math.ceil(math.sqrt(len(base_data)))
        if n * (n - 1) >= len(base_data): m -= 1

        fig, axs = self.subplots2d(n, m)
        for i, filename in enumerate(base_data):
            ax = axs[i // m, i % m]
            ax.set_title(filename)
            print(f'Subplot #{i}: {filename}')

            for line_data in base_data[filename]:
                if plot_params[0] != '-' and plot_params[0] != line_data: continue
                ax.plot(base_data[filename][line_data]['timings'], label=line_data)

            ax.legend()

        plt.tight_layout()
        self.update_canvas(fig)

    def plot_groups_framings(self, *plot_params):
        data = self.timing_data[group_name]

        n = m = math.ceil(math.sqrt(len(data)))
        if n * (n - 1) >= len(data): m -= 1

        fig, axs = plt.subplots(n, m, figsize=(n*6, m*4))
        if not hasattr(axs, '__len__'): axs = np.array([[axs]])
        if not hasattr(axs[0], '__len__'): axs = np.array([axs]).reshape(-1, 1)
        for i, filename in enumerate(data):
            ax = axs[i // m, i % m]
            ax.set_title(filename)

            for line_data in data[filename]:
                # framings = convert_to_framings(line_data['timings'])
                timings = np.cumsum(line_data['timings'])
                stop_time = int(math.ceil(timings[-1]))
                ax.hist(timings, bins=stop_time, range=(0, stop_time), label=line_data['dir'], alpha=0.5)

            ax.legend()

        plt.tight_layout()
        self.update_canvas(fig)

    def plot_composite(self, *plot_params):
        data = self.timing_data[group_name]

        fig = plt.figure()
        composite_data = { }

        for i, filename in enumerate(data):
            for line_data in data[filename]:
                if line_data['dir'] not in composite_data:
                    composite_data[line_data['dir']] = { 'timings': line_data['timings'], 'separators': [len(line_data['timings'])] }
                else:
                    composite_data[line_data['dir']]['timings'] = np.concatenate((composite_data[line_data['dir']]['timings'], line_data['timings']))
                    composite_data[line_data['dir']]['separators'].append(composite_data[line_data['dir']]['separators'][-1] + len(line_data['timings']))

        total_data = []
        for plot_name in composite_data:
            color = None
            if not self.draw_only_smoothed():
                color = plt.plot(composite_data[plot_name]['timings'], label=plot_name, alpha=0.35, lw=1)[0].get_color()

            total_data = np.concatenate((total_data, composite_data[plot_name]['timings']))
            smoothed = smooth_array(composite_data[plot_name]['timings'], window_size=35)
            plt.plot(smoothed, label=plot_name, color=color)

        total_data = np.sort(total_data)
        top_limit = total_data[int(len(total_data) * 0.999 - 0.999)] * 1.01
        bottom_limit = total_data[0] / 1.01
        plt.ylim(bottom_limit, top_limit)

        plt.legend()
        self.update_canvas(fig)

    def on_closing(self):
        self.root.destroy()
        sys.exit(0) # works for now ig
    
    def __init__(self, reports_dir):
        self.reports_dir = reports_dir

    def main(self):
        # prepare data
        self.parse_data(self.reports_dir)

        # setup the window
        self.root = tk.Tk()
        self.root.title("Constellation report analyzer")

        # frame for control UIs
        self.top_frame = tk.Frame(self.root)
        self.top_frame.pack(fill=tk.X)

        # Create a dropdown menu for selecting plotting mode
        self.mode_var = tk.StringVar()
        plot_modes = ['separate', 'composite', 'framings']
        mode_menu = ttk.OptionMenu(self.top_frame, self.mode_var, 'composite', *plot_modes, command=lambda x: self.update_plots())
        mode_menu.pack(side=tk.RIGHT, fill=tk.X, expand=True)

        # Checkbox for showing only smoothed version of plot vs. smoothed + raw
        self.only_smoothed_var = tk.BooleanVar(value=False)
        checkbox = tk.Checkbutton(self.top_frame, text="Only smoothed", variable=self.only_smoothed_var, command=lambda x: self.update_plots())
        checkbox.pack(side=tk.RIGHT, fill=tk.X, expand=True)

        # canvas is created elsewhere..
        self.canvas = None

        # dynamically generated UI controls
        self.enumerate_groups(self.timing_data)

        # finish setup and start
        self.root.protocol("WM_DELETE_WINDOW", lambda: self.on_closing())
        self.root.mainloop()

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description='Plot timings from two directories')
    parser.add_argument('dir', help='Path to the first directory')
    args = parser.parse_args()

    app = ReportAnalyzer(args.dir)
    app.main()
