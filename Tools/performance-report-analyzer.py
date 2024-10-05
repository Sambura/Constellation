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

# UI controls
canvas = None
root = None

# data
timing_data = None
data_groups = None

def is_integer(s):
    try:
        int(s)
        return True
    except ValueError:
        return False

def parse_groups(dir1, dir2):
    global data_groups

    files1 = [f for f in os.listdir(dir1) if f.endswith('.json')]
    files2 = [f for f in os.listdir(dir2) if f.endswith('.json')]

    groups = set()
    for file in files1 + files2:
        parts = file.split('-')
        if len(parts) > 2 and is_integer(parts[-2]):
            prefix = '-'.join(parts[:-2])
        else:
            prefix = file
        groups.add(prefix)

    data_groups = list(groups)

def parse_file(path):
    data = {}

    with open(path, 'r') as file:
        json_data = json.load(file)

    data['timings'] = np.array(json_data['Timings'].split(','), dtype=float)
    data['filename'] = os.path.basename(os.path.basename(path))
    data['json_data'] = json_data

    return data

def parse_data(*dirs):
    global timing_data
    parse_groups(dirs[0], dirs[1])

    timing_data = { group: {} for group in data_groups }
    for group in data_groups:
        for dirpath in dirs:
            filenames = [f for f in os.listdir(dirpath) if f.startswith(group) and f.endswith('.json')]

            for file in filenames:
                filename = os.path.basename(file)
                file_data = parse_file(os.path.join(dirpath, file))

                if filename not in timing_data[group]: timing_data[group][filename] = []
                timing_data[group][filename].append({ 'dir': os.path.basename(dirpath), **file_data})

######################################################################################

def smooth_array(arr, window_size=3):
    # Calculate the padding size to ensure the output array has the same shape
    padding_size = window_size // 2

    # Pad the input array with zeros to handle edge cases
    padded_arr = np.pad(arr, (padding_size, padding_size), mode='edge')

    # Apply the moving average filter
    smoothed_arr = np.convolve(padded_arr, np.ones(window_size) / window_size, mode='valid')

    return smoothed_arr

def update_canvas(new_fig):
    global canvas
    if canvas is not None: canvas.get_tk_widget().pack_forget()
    canvas = FigureCanvasTkAgg(new_fig, master=root)
    canvas.draw()
    canvas.get_tk_widget().pack(side=tk.BOTTOM, fill=tk.BOTH, expand=1)

def convert_to_framings(data):
    cum_data = np.cumsum(data)
    return [np.sum(np.logical_and(cum_data < i + 1, cum_data >= i )) for i in range(int(math.ceil(cum_data[-1])))]

def plot_groups(group_name):
    data = timing_data[group_name]

    n = m = math.ceil(math.sqrt(len(data)))
    if n * (n - 1) >= len(data): m -= 1

    fig, axs = plt.subplots(n, m, figsize=(n*6, m*4))
    if not hasattr(axs, '__len__'): axs = np.array([[axs]])
    if not hasattr(axs[0], '__len__'): axs = np.array([axs]).reshape(-1, 1)
    for i, filename in enumerate(data):
        ax = axs[i // m, i % m]
        ax.set_title(filename)

        for line_data in data[filename]:
            ax.plot(line_data['timings'], label=line_data['dir'])
        
        ax.legend()
            
    plt.tight_layout()
    update_canvas(fig)

def plot_groups_framings(group_name):
    data = timing_data[group_name]

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
    update_canvas(fig)

def plot_composite(group_name):
    data = timing_data[group_name]

    fig = plt.figure()
    composite_data = { }

    for i, filename in enumerate(data):
        for line_data in data[filename]:
            if line_data['dir'] not in composite_data:
                composite_data[line_data['dir']] = { 'timings': line_data['timings'], 'separators': [len(line_data['timings'])] }
            else:
                composite_data[line_data['dir']]['timings'] = np.concatenate((composite_data[line_data['dir']]['timings'], line_data['timings']))
                composite_data[line_data['dir']]['separators'].append(composite_data[line_data['dir']]['separators'][-1] + len(line_data['timings']))
    
    for plot_name in composite_data:
        line, = plt.plot(composite_data[plot_name]['timings'], label=plot_name, alpha=0.35, lw=1)
        smoothed = smooth_array(composite_data[plot_name]['timings'], window_size=35)
        plt.plot(smoothed, label=plot_name, color=line.get_color())
    
    plt.legend()
    update_canvas(fig)

def on_closing():
    print('destroying...')
    root.destroy()
    sys.exit(0) # works for now ig

def main(dir1, dir2):
    global root

    # Create a Tkinter window
    root = tk.Tk()
    root.title("Plot Timings")
    parse_data(dir1, dir2)

    # Create a dropdown menu
    group_var = tk.StringVar()
    mode_var = tk.StringVar()

    def update_plot():
        if mode_var.get() == 'composite':
            plot_composite(group_var.get())
        elif mode_var.get() == 'framings':
            plot_groups_framings(group_var.get())
        else:
            plot_groups(group_var.get())

    group_menu = ttk.OptionMenu(root, group_var, 'x', *data_groups, command=lambda x: update_plot())
    group_menu.pack(side=tk.TOP)

    mode_menu = ttk.OptionMenu(root, mode_var, 'x', *['separate', 'composite', 'framings'], command=lambda x: update_plot())
    mode_menu.pack(side=tk.TOP)

    root.protocol("WM_DELETE_WINDOW", on_closing)
    root.mainloop()

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description='Plot timings from two directories')
    parser.add_argument('dir1', help='Path to the first directory')
    parser.add_argument('dir2', help='Path to the second directory')
    args = parser.parse_args()

    main(args.dir1, args.dir2)
