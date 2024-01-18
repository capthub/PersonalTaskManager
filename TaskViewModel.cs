using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;

namespace PersonalTaskManager
{
    public class TaskViewModel : ViewModelBase
    {
        private PtmDbContext _dbContext;
        private DateTime _selectedDate;
        private string _newTaskDescription;

        public TaskData TaskData { get; } = new TaskData();

        public TaskModel SelectedTask { get; set; }

        public string NewTaskDescription
        {
            get => _newTaskDescription;
            set => SetProperty(ref _newTaskDescription, value);
        }

        public ICommand AddTaskCommand { get; }
        public ICommand UpdateTaskCommand { get; }
        public ICommand DeleteTaskCommand { get; }
        public ICommand LoadTaskCommand { get; }

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                _selectedDate = value;
                LoadTasks();
            }
        }

        public TaskViewModel()
        {
            _dbContext = new PtmDbContext();
            AddTaskCommand = new RelayCommand(AddTask);
            UpdateTaskCommand = new RelayCommand(UpdateTask);
            DeleteTaskCommand = new RelayCommand(DeleteTask);
            LoadTaskCommand = new RelayCommand(LoadTasks);
            // Default Date
            SelectedDate = DateTime.Now.Date;
        }

        private void AddTask()
        {
            var inputDialog = new InputDialog();
            if (inputDialog.ShowDialog() == true)
            {
                string newTaskDescription = inputDialog.UserInput;
                bool checkUpcoming = SelectedDate < DateTime.Today.AddDays(3) && SelectedDate > DateTime.Today;
                var newTask = new TaskModel { Description = newTaskDescription, Date = SelectedDate, isUpcoming = checkUpcoming };

                _dbContext.Tasks.Add(newTask);
                _dbContext.SaveChanges();
                LoadTasks(); // Refresh the task list
            }
        }

        private void LoadTasks()
        {
            TaskData.Tasks.Clear();
            var tasksForTheDay = GetTasksForDay(SelectedDate);

            foreach (var t in tasksForTheDay)
            {
                TaskData.Tasks.Add(t);
            }

            DisplayUpcomingTasksMessage();
        }

        private void DisplayUpcomingTasksMessage()
        {
            var upcomingTaskCount = 0;
            var tasksInRange = _dbContext.Tasks
                .Where(t => t.Date >= DateTime.Today.Date && t.Date <= DateTime.Today.AddDays(3)).ToList();
            foreach (var t in tasksInRange)
            {
                int daysDifference = (t.Date - DateTime.Today).Days;
                bool isUpcoming = daysDifference > 0 && daysDifference <= 3;
                t.isUpcoming = isUpcoming; // Check whether the task is upcoming on loading
                if (isUpcoming)
                {
                    upcomingTaskCount++;
                }
            }
            if (upcomingTaskCount > 0)
            {
                TaskData.UpcomingTasksMessage = $"Ilość zbliżających się zadań w ciągu 3 dni: {upcomingTaskCount}";
            }
            else
            {
                TaskData.UpcomingTasksMessage = "Nie masz nadchodzących zadań ciągu przyszłych 3 dni";
            }
        }

        private void UpdateTask()
        {
            if (SelectedTask != null)
            {
                var inputDialog = new InputDialog();
                if (inputDialog.ShowDialog() == true)
                {
                    string newDescription = inputDialog.UserInput;

                    SelectedTask.Description = newDescription;

                    _dbContext.Entry(SelectedTask).State = EntityState.Modified;
                    _dbContext.SaveChanges();
                    LoadTasks(); // Refresh the task list
                }
            }
        }

        private void DeleteTask()
        {
            if (SelectedTask != null)
            {
                _dbContext.Tasks.Remove(SelectedTask);
                _dbContext.SaveChanges();
                LoadTasks(); // Refresh the task list
            }
        }

        private List<TaskModel> GetTasksForDay(DateTime selectedDate)
        {
            try
            {
                return _dbContext.Tasks
                    .Where(t => t.Date >= selectedDate.Date && t.Date < selectedDate.Date.AddDays(1)).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception {ex.Message}");
                throw;
            }
        }
    }

    public class TaskData : INotifyPropertyChanged
    {
        private ObservableCollection<TaskModel> _tasks = new ObservableCollection<TaskModel>();
        private string _upcomingTaskMessage = string.Empty;

        public ObservableCollection<TaskModel> Tasks
        {
            get { return _tasks; }
            set
            {
                if (_tasks != value)
                {
                    _tasks = value;
                    OnPropertyChanged(nameof(Tasks));
                }
            }
        }

        public string UpcomingTasksMessage
        {
            get { return _upcomingTaskMessage; }
            set
            {
                if (_upcomingTaskMessage != value)
                {
                    _upcomingTaskMessage = value;
                    OnPropertyChanged(nameof(UpcomingTasksMessage));
                }
            }
        }

        public TaskData()
        {
            _tasks = new ObservableCollection<TaskModel>();
            _upcomingTaskMessage = string.Empty;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Custom implementation to bind commands to controls in the View
    public class RelayCommand : ICommand
    {
            private readonly Action _execute;
            private readonly Func<bool> _canExecute;

            public event EventHandler CanExecuteChanged;

            public RelayCommand(Action execute, Func<bool> canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public bool CanExecute(object parameter)
            {
                return _canExecute == null || _canExecute();
            }

            public void Execute(object parameter)
            {
                _execute();
            }

            public void RaiseCanExecuteChanged()
            {
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
    }
}