# IMPORTS
import matplotlib
matplotlib.use('TkAgg')
from matplotlib import pyplot as plt
import numpy as np
from statistics import *

def save_plots(path):
    lines = []
    with open(path + "info.txt", "r") as f:
        lines = f.readlines()

    class_loss = []
    class_val_loss = []
    acc = []

    for i, line in enumerate(lines):
        if i == 0:
            continue
        split = line.split(" ")
        class_loss.append(float(split[3][:-1]))
        class_val_loss.append(float(split[5][:-1]))
        acc.append(float(split[7][:-1]))

    fig, ax1 = plt.subplots(figsize=(10, 5))
    ax1.plot(class_loss, label='Regression Model Training Loss', color='b')
    ax1.set_xlabel('Epoch')
    ax1.set_ylabel('Regression Model Training Loss', color='b')
    ax1.tick_params(axis='y', labelcolor='b')
    ax2 = ax1.twinx()
    ax2.plot(class_val_loss, label='Regression Model Validation Loss', color='r')
    ax2.set_ylabel('Regression Model Validation Loss', color='r')
    ax2.tick_params(axis='y', labelcolor='r')
    plt.grid()
    plt.savefig(path + "Loss.png")
    plt.close()

    plt.figure()
    plt.plot(acc, color='black') #plot the data
    plt.ylabel('Accuracy') #set the label for y axis
    plt.xlabel('Step') #set the label for x-axis
    plt.grid()
    plt.savefig(path + "Acc.png")
    plt.close()

def save_plots_combined(path):
    lines = []
    with open(path + "info.txt", "r") as f:
        lines = f.readlines()

    goal_loss = []
    goal_val_loss = []
    goal_acc = []

    group_loss = []
    group_val_loss = []
    group_acc = []

    total_acc = []

    for i, line in enumerate(lines):
        if i == 0:
            continue
        split = line.split(" ")
        goal_loss.append(float(split[3][:-1]))
        group_loss.append(float(split[5][:-1]))
        goal_val_loss.append(float(split[7][:-1]))
        group_val_loss.append(float(split[9][:-1]))
        goal_acc.append(float(split[11][:-1]))
        group_acc.append(float(split[13][:-1]))
        total_acc.append(float(split[15][:-1]))


    # Goal Loss
    fig, ax1 = plt.subplots(figsize=(10, 5))
    ax1.plot(goal_loss, label='Goal Training Loss', color='b')
    ax1.set_xlabel('Epoch')
    ax1.set_ylabel('Goal Training Loss', color='b')
    ax1.tick_params(axis='y', labelcolor='b')
    ax2 = ax1.twinx()
    ax2.plot(goal_val_loss, label='Goal Validation Loss', color='r')
    ax2.set_ylabel('Goal Validation Loss', color='r')
    ax2.tick_params(axis='y', labelcolor='r')
    plt.grid()
    plt.title("Goal Loss")
    plt.savefig(path + "Goal_Loss.png")
    plt.close()

    # Total Loss
    fig, ax1 = plt.subplots(figsize=(10, 5))
    ax1.plot(group_loss, label='Total Training Loss', color='b')
    ax1.set_xlabel('Epoch')
    ax1.set_ylabel('Total Training Loss', color='b')
    ax1.tick_params(axis='y', labelcolor='b')
    ax2 = ax1.twinx()
    ax2.plot(group_val_loss, label='Total Validation Loss', color='r')
    ax2.set_ylabel('Total Validation Loss', color='r')
    ax2.tick_params(axis='y', labelcolor='r')
    plt.grid()
    plt.title("Total Loss")
    plt.savefig(path + "Total_Loss.png")
    plt.close()

    # Goal Accuracy
    plt.figure()
    plt.plot(goal_acc, color='red', label="Goal Accuracy") #plot the data
    plt.plot(group_acc, color='blue', label="Group Accuracy") #plot the data
    plt.plot(total_acc, color='black', label="Total Accuracy") #plot the data
    plt.ylabel('Accuracy') #set the label for y axis
    plt.xlabel('Step') #set the label for x-axis
    plt.grid()
    plt.legend()
    plt.savefig(path + "Acc.png")
    plt.close()

def save_plots_all(path):
    lines = []
    with open(path + "info.txt", "r") as f:
        lines = f.readlines()

    loss = []
    val_loss = []
    total_acc = []
    second_acc = []
    goal_acc = []
    group_acc = []
    interact_acc = []
    connect_acc = []

    for i, line in enumerate(lines):
        if i == 0:
            continue
        split = line.split(" ")
        loss.append(float(split[3][:-1]))
        val_loss.append(float(split[5][:-1]))
        total_acc.append(float(split[7][:-1]))
        second_acc.append(float(split[9][:-1]))
        goal_acc.append(float(split[11][:-1]))
        group_acc.append(float(split[13][:-1]))
        interact_acc.append(float(split[15][:-1]))
        connect_acc.append(float(split[17][:-1]))

    fig, ax1 = plt.subplots(figsize=(10, 5))
    ax1.plot(loss, label='Training Loss', color='b')
    ax1.set_xlabel('Epoch')
    ax1.set_ylabel('Training Loss', color='b')
    ax1.tick_params(axis='y', labelcolor='b')
    ax2 = ax1.twinx()
    ax2.plot(val_loss, label='Validation Loss', color='r')
    ax2.set_ylabel('Validation Loss', color='r')
    ax2.tick_params(axis='y', labelcolor='r')
    plt.grid()
    plt.savefig(path + "Loss.png")
    plt.close()

    # Goal Accuracy
    plt.figure()
    plt.plot(goal_acc, color='red', label="A Accuracy") #plot the data
    plt.plot(group_acc, color='blue', label="B Accuracy") #plot the data
    plt.plot(interact_acc, color='green', label="C Accuracy") #plot the data
    plt.plot(connect_acc, color='black', label="Connect Accuracy") #plot the data
    plt.plot(total_acc, color='cyan', label="First Accuracy") #plot the data
    plt.plot(second_acc, color='pink', label="Second Accuracy") #plot the data
    plt.ylabel('Accuracy') #set the label for y axis
    plt.xlabel('Step') #set the label for x-axis
    plt.grid()
    plt.legend()
    plt.savefig(path + "Acc.png")
    plt.close()

def save_plots_all_weights(path):
    lines = []
    with open(path + "info.txt", "r") as f:
        lines = f.readlines()

    loss = []
    val_loss = []
    total_acc = []
    second_acc = []
    goal_acc = []
    group_acc = []
    interact_acc = []
    connect_acc = []

    for i, line in enumerate(lines):
        if i == 0:
            continue
        split = line.split(" ")
        loss.append(float(split[3][:-1]))
        val_loss.append(float(split[5][:-1]))
        total_acc.append(float(split[7][:-1]))
        second_acc.append(float(split[9][:-1]))
        goal_acc.append(float(split[11][:-1]))
        group_acc.append(float(split[13][:-1]))
        interact_acc.append(float(split[15][:-1]))
        connect_acc.append(float(split[17][:-1]))

    fig, ax1 = plt.subplots(figsize=(10, 5))
    ax1.plot(loss, label='Training Loss', color='b')
    ax1.set_xlabel('Epoch')
    ax1.set_ylabel('Training Loss', color='b')
    ax1.tick_params(axis='y', labelcolor='b')
    ax2 = ax1.twinx()
    ax2.plot(val_loss, label='Validation Loss', color='r')
    ax2.set_ylabel('Validation Loss', color='r')
    ax2.tick_params(axis='y', labelcolor='r')
    plt.grid()
    plt.savefig(path + "Loss.png")
    plt.close()

    # Goal Accuracy
    plt.figure()
    plt.plot(goal_acc, color='red', label="Goal Accuracy") #plot the data
    plt.plot(group_acc, color='blue', label="Group Accuracy") #plot the data
    plt.plot(interact_acc, color='green', label="Interact Accuracy") #plot the data
    plt.plot(connect_acc, color='black', label="Connect Accuracy") #plot the data
    plt.plot(total_acc, color='cyan', label="First Accuracy") #plot the data
    plt.plot(second_acc, color='pink', label="Second Accuracy") #plot the data
    plt.ylabel('Accuracy') #set the label for y axis
    plt.xlabel('Step') #set the label for x-axis
    plt.grid()
    plt.legend()
    plt.savefig(path + "Acc.png")
    plt.close()

def moving_average(data, window_size):
    '''Compute moving average of the data with a given window size'''
    return np.convolve(data, np.ones(window_size)/window_size, mode='valid')

def save_plots_all_weights_DEMO(path):
    lines = []
    with open(path + "info.txt", "r") as f:
        lines = f.readlines()

    loss = []
    val_loss = []
    total_acc = []
    second_acc = []
    goal_acc = []
    group_acc = []
    interact_acc = []
    connect_acc = []

    for i, line in enumerate(lines):
        if i == 0 or i > 510:
            continue
        split = line.split(" ")
        loss.append(float(split[3][:-1]))
        val_loss.append(float(split[5][:-1]))
        total_acc.append(float(split[7][:-1]) * 100)
        second_acc.append(float(split[9][:-1]) * 100)
        goal_acc.append(float(split[11][:-1]) * 100)
        group_acc.append(float(split[13][:-1]) * 100)
        interact_acc.append(float(split[15][:-1]) * 100)
        connect_acc.append(float(split[17][:-1]) * 100)

    # Goal Accuracy
    fig, ax = plt.subplots(figsize=(6, 7))
    # Apply smoothing
    window_size = 10  # you can adjust this value as needed
    smooth_goal_acc = moving_average(goal_acc, window_size)
    smooth_group_acc = moving_average(group_acc, window_size)
    smooth_interact_acc = moving_average(interact_acc, window_size)
    smooth_connect_acc = moving_average(connect_acc, window_size)
    smooth_total_acc = moving_average(total_acc, window_size)
    smooth_second_acc = moving_average(second_acc, window_size)

    # Plot the smoothed data
    ax.plot(smooth_total_acc, color='cornflowerblue', label="1st most dominant", linewidth=3.0)
    ax.plot(smooth_second_acc, color='gray', label="2nd most dominant", linewidth=3.0)
    ax.plot(smooth_goal_acc, color='red', label="Goal Seeking", linewidth=1.25)
    ax.plot(smooth_group_acc, color='blue', label="Grouping", linewidth=1.25)
    ax.plot(smooth_interact_acc, color='green', label="Interaction", linewidth=1.25)
    ax.plot(smooth_connect_acc, color='black', label="Connectivity", linewidth=1.25)
    plt.ylabel('Accuracy %', fontsize=15) #set the label for y axis
    plt.xlabel('Step', fontsize=15) #set the label for x-axis
    plt.title("Validation Accuracies", fontsize=17)
    plt.grid()
    plt.legend(fontsize=12)
    plt.show()

if __name__ ==  '__main__':
    save_plots_all_weights_DEMO("output_Full/saved_info/")