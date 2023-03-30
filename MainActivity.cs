using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Widget;
using AndroidX.AppCompat.App;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Todoist.Net;
using Todoist.Net.Models;

namespace TodoistShifter
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private static readonly string DATE_SEPARATOR = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.DateSeparator;
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        // TODOIST Settings
        private const string TOKEN = "591120e00e2378e13011380577eb508045cb753b";
        private const string PROJECT_NAME = "Тренировка";
        private readonly string[] TASK_NAMES = { "Грудь", "Спина", "Пресс" };

        private TodoistClient todoistClient;
        private Button minus, plus;
        private Project project;

        protected async override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            todoistClient = new TodoistClient(TOKEN);

            project = await GetProject(PROJECT_NAME);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);

            SetContentView(Resource.Layout.activity_main);
            SetRefs();
            SetEvents();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private void SetRefs()
        {
            minus = FindViewById<Button>(Resource.Id.minusButton);
            plus = FindViewById<Button>(Resource.Id.plusButton);
        }

        private void SetEvents()
        {
            minus.Click += Minus_Click;
            plus.Click += Plus_Click;
        }

        private async Task<List<Item>> GetTasks(Project inProject)
        {
            List<Item> toUpdateTasks = new List<Item>();

            var tasks = await todoistClient.Items.GetAsync();

            foreach (var task in tasks)
            {
                if (task.ProjectId == inProject.Id)
                {
                    if (TASK_NAMES.Contains(task.Content))
                        toUpdateTasks.Add(task);
                }
            }
            return toUpdateTasks;
        }

        private async Task<Project> GetProject(string name)
        {
            var projects = await todoistClient.Projects.GetAsync();
            return projects.FirstOrDefault(p => p.Name == name);
        }

        private async void Plus_Click(object sender, System.EventArgs e)
        {
            await semaphore.WaitAsync();

            try
            {
                foreach (var task in await GetTasks(project))
                {
                    task.DueDate = new DueDate($"{task.DueDate.Date.Value.AddDays(1).ToShortDateString().Replace(DATE_SEPARATOR, "/")} every day");
                    await todoistClient.Items.UpdateAsync(task);
                }
            }
            finally
            {
                semaphore.Release();
            }
            Toast.MakeText(Application.Context, "Updated Successfully", ToastLength.Long).Show();
        }

        private async void Minus_Click(object sender, System.EventArgs e)
        {
            await semaphore.WaitAsync();
            try
            {
                foreach (var task in await GetTasks(project))
                {
                    task.DueDate = new DueDate($"{task.DueDate.Date.Value.AddDays(-1).ToShortDateString().Replace(DATE_SEPARATOR, "/")} every day");
                    await todoistClient.Items.UpdateAsync(task);
                }
            }
            finally
            {
                semaphore.Release();
            }
            Toast.MakeText(Application.Context, "Updated Successfully", ToastLength.Long).Show();
        }
    }
}