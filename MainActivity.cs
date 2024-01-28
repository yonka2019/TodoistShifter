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
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher =
        true)]
    public class MainActivity : AppCompatActivity
    {
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        // -- Settings --
        private const string TOKEN = "62f055d6fe557cf55e45ece12cb1cb04eb04f06e";  // Todoist API token (could be taken from Todoist application)
        private const string PROJECT_NAME = "Gym";  // project name where the tasks stored
        private readonly string[] TASK_NAMES = { "Chest", "Back" };  // name of tasks to shift
        private const string RECURRING_TEXT = "every 3 days";  // text to enable recurring (not necessary) ; for example : 'every 3 days'
        private const int SHIFT_BY = 1;  // shift the given tasks by: (+ / - days)
        // -- --- --

        private TodoistClient todoistClient;
        private Button minus, plus;
        private Project project;
        private ListView tasksLV;

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            try
            {
                todoistClient = new TodoistClient(TOKEN);
                project = await GetProject(PROJECT_NAME);
            }
            catch (System.Exception ex)
            {
                Android.App.AlertDialog.Builder dialog = new Android.App.AlertDialog.Builder(this);
                Android.App.AlertDialog alert = dialog.Create();

                alert.SetTitle("Can't authenticate into Todoist");
                alert.SetMessage(ex.Message.ToString() + "\n\r" + ex.StackTrace.ToString() + "\n\r" + "\n\r");

                alert.SetButton("OK", (c, ev) =>
                {
                    Finish();
                });
                alert.Show();

                return;
            }

            Toast.MakeText(Application.Context, "Logged In", ToastLength.Long).Show();

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);

            SetContentView(Resource.Layout.activity_main);
            SetRefs();
            SetEvents();

            int[] to = { Resource.Id.textView1 };
            JavaList<IDictionary<string, object>> data = new JavaList<IDictionary<string, object>>();

            for (int i = 0; i < TASK_NAMES.Length; i++)
            {
                JavaDictionary<string, object> dict = new JavaDictionary<string, object>
                {
                    { "TASK", TASK_NAMES[i] }
                };

                data.Add(dict);
            }
            SimpleAdapter adapter = new SimpleAdapter(this, data, Resource.Layout.custom_list_item, new string[] { "TASK" }, to);

            tasksLV.Adapter = adapter;
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
            tasksLV = FindViewById<ListView>(Resource.Id.tasksLV);
        }

        private void SetEvents()
        {
            minus.Click += Minus_Click;
            plus.Click += Plus_Click;
        }

        private async Task<List<Item>> GetTasks(Project inProject)
        {
            List<Item> toUpdateTasks = new List<Item>();

            IEnumerable<Item> tasks = await todoistClient.Items.GetAsync();

            foreach (Item task in tasks)
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
            IEnumerable<Project> projects = await todoistClient.Projects.GetAsync();
            return projects.FirstOrDefault(p => p.Name == name);
        }

        private async void Plus_Click(object sender, System.EventArgs e)
        {
            Toast.MakeText(Application.Context, "Trying to update..", ToastLength.Long).Show();

            await semaphore.WaitAsync();
            try
            {
                foreach (Item task in await GetTasks(project))
                {
                    task.DueDate = new DueDate(RECURRING_TEXT, task.DueDate.Date.Value.AddDays(SHIFT_BY), true);
                    await todoistClient.Items.UpdateAsync(task);
                }

                Toast.MakeText(Application.Context, "Updated Successfully", ToastLength.Long).Show();
            }
            catch (System.Exception ex)
            {
                Toast.MakeText(Application.Context, $"Error: {ex.Message}", ToastLength.Long).Show();
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async void Minus_Click(object sender, System.EventArgs e)
        {
            Toast.MakeText(Application.Context, "Trying to update..", ToastLength.Long).Show();

            await semaphore.WaitAsync();
            try
            {
                foreach (Item task in await GetTasks(project))
                {
                    task.DueDate = new DueDate(RECURRING_TEXT, task.DueDate.Date.Value.AddDays(SHIFT_BY * -1), true);
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