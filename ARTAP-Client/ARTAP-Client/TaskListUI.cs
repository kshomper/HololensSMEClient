﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WpfApplication1
{
    public class TaskListUI
    {
        public TaskList TaskList { get; set; }
        public List<TaskUI> TaskUIs { get; set; }
        public Button Button { get; set; }
        public TextBox NameTextBox { get; set; }

        public TaskListUI(TaskList taskList, Style style)
        {
            TaskList = taskList;

            TaskUIs = new List<TaskUI>();

            var name = $"List{TaskList.Id}";
            Button = new Button
            {
                Content = $"list{TaskList.Id}",
                Height = 30,
                Name = name,
                Tag = name,
                VerticalAlignment = VerticalAlignment.Top,
                Width = 150
            };

            name = TaskList.Name;
            NameTextBox = new TextBox
            {
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Name = name,
                Text = name,
                VerticalAlignment = VerticalAlignment.Top,
            };

            AddTaskUI(style, 60);
        }

        public void AddTaskUI(Style style, int margin)
        {
            TaskUIs.Add(new TaskUI(new Task(), margin, style));
        }

        public void RemoveTaskUI(TaskUI uiTask, Grid taskGrid)
        {
            taskGrid.Children.Remove(uiTask.IsCompletedUI);
            taskGrid.Children.Remove(uiTask.NameUI);
            taskGrid.Children.Remove(uiTask.Remove);

            TaskUIs.Remove(uiTask);
            TaskList.Tasks.Remove(uiTask.Task);
        }

        public void SetTaskUIMargins(int margin)
        {
            var offset = 0;
            foreach (var taskUI in TaskUIs)
            {
                taskUI.SetMargins(margin + offset);
                offset += 30;
            }
        }

        public void RecreateUIElements(Style style)
        {
            var name = TaskList.Name;
            NameTextBox = new TextBox
            {
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Name = name,
                Text = name,
                VerticalAlignment = VerticalAlignment.Top,
            };

            var margin = 60;
            foreach (var taskUI in TaskUIs)
            {
                taskUI.RecreateUIElements(margin, style);
                margin += 30;
            }
        }
    }
}
