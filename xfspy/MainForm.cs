using System;
using System.Collections;
using Eto.Forms;
using Eto.Drawing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
// warnings make me go insane
// ReSharper disable all
using System.Diagnostics;
using System.Linq;

namespace xfspy
{
	public partial class MainForm : Form
	{
		
		/// <summary>
		/// The MainForm class deriving from Form is the entry point of the graphical user interface of the application. It handles the primary window attributes and runtime events.
		/// Overview of MainForm constructor behaviors:
		/// 1. Starts a new Process which runs the "xfconf-query -l" command in the bash in order to get a list of all channels.
		/// 2. Filter out any unnecessary information from the captured lines of output from the aforementioned Process.
		/// 3. Starts an asynchronous Task which repeatedly runs the "xfconf-query -cm" command for each fetched channel.
		/// 4. Sets the title and size constraints for the MainForm window.
		/// 5. Defines and adds specific commands and events associated with various components present inside the MainForm such as buttons and menus.
		/// Remember, the application can asynchronously monitor multiple channels using "xfconf-query -cm" commands and log the result to the console.
		/// </summary>
        string currentLine = "";
		string prevLine = "";
		
		public int FindStartIndex(string target, string[] prefixes)
		{
			for (int i = 0; i < prefixes.Length; i++)
			{
				if (target.StartsWith(prefixes[i]))
				{
					return i;
				}
			}
			return -1; // Not found
		}
		
		public MainForm()
		{
			// So if the program crashed or had a Ctrl+C moment, there would not be extra xfconf-query instances
			Process.Start("bash", "-c \"killall xfconf-query\"");
			
			// Parse xfconf-query -l to get a list of all channels
			string cmd = "-c \"xfconf-query -l\"";
			System.Diagnostics.ProcessStartInfo procStartInfo = new System.Diagnostics.ProcessStartInfo("bash", cmd)
			{
			    RedirectStandardOutput = true,
			    UseShellExecute = false,
			    CreateNoWindow = true
			};

			System.Diagnostics.Process proc = new System.Diagnostics.Process();
			proc.StartInfo = procStartInfo;
			proc.Start();
			string result = proc.StandardOutput.ReadToEnd();
			string[] lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
			List<string> filteredLines = new List<string>();
			for (int i = 1; i < lines.Length-1; i++)
			{
			   filteredLines.Add(lines[i].Remove(0,2));
			}
			string[] channels = filteredLines.ToArray();
			// end
			
			// Create new task run async xfconf-query -m command
			Task.Run(() =>
			{
			    var tasks = new List<Task>();
		
			    foreach (string channel in channels)
			    {
			        tasks.Add(Task.Run(() =>
			        {
			            string monitorCmd = $"-c \"xfconf-query -cm {channel}\"";
			            var monitorProcStartInfo = new System.Diagnostics.ProcessStartInfo("bash", monitorCmd)
			            {
			                RedirectStandardOutput = true,
			                UseShellExecute = false,
			                CreateNoWindow = true
			            };
		
			            var monitorProc = new System.Diagnostics.Process
			            {
			                StartInfo = monitorProcStartInfo
			            };
		
			            monitorProc.Start();
		
			            while (true)
			            {
			                string monitorResult = monitorProc.StandardOutput.ReadLine();
							if(monitorResult != null && monitorResult != "") {
								if (!monitorResult.Contains("Start monitoring")) {
									// Start the child process.
									Process p = new Process();
									// Redirect the output stream of the child process.
									p.StartInfo.UseShellExecute = false;
									p.StartInfo.RedirectStandardOutput = true;
									p.StartInfo.CreateNoWindow = true;
									p.StartInfo.FileName = "bash";
									p.StartInfo.Arguments = $"-c \"xfconf-query -c {channel} -p {monitorResult.Split(' ')[1]}";
									p.Start();
									string output = p.StandardOutput.ReadToEnd();
									p.WaitForExit();
									currentLine = $"{channel}>{monitorResult}>{output.Replace("\n","")}";
									Console.WriteLine(currentLine);
								} else
								{
									currentLine = $"{channel}>{monitorResult}";
									Console.WriteLine(currentLine);
								}
							}
			            }
			        }));
			    }
		
			    Task.WhenAll(tasks).Wait();
			});
			//end
			
			// GUI
			Title = "xfspy";
			MinimumSize = new Size(300, 200);
			Size = new Size(900,400);
			var clickMe = new Command { MenuText = "Click Me!", ToolBarText = "Click Me!" };
			clickMe.Executed += (sender, e) => MessageBox.Show(this, "I was clicked!");
			
			var listBox = new ListBox {};
			
			// create status bar
			var status = new Label {Text = "Not Ready!"};
			var prog = new ProgressBar {MaxValue = channels.Length-1, Width = 100};
			var statusBar = new Panel
			{
				// definitely not stolen from likeNotepad
				Padding = new Padding(5),
				Content = new StackLayout
				{
					Orientation = Orientation.Horizontal,
					HorizontalContentAlignment = HorizontalAlignment.Stretch, // Stretch to ensure full width
					Spacing = 5,
					Items =
					{
						new StackLayoutItem { Expand = true }, // Empty item to push the label to the right
						new StackLayoutItem(status, HorizontalAlignment.Right),
						new StackLayoutItem(prog, HorizontalAlignment.Right)
					}
				},
				BackgroundColor = listBox.BackgroundColor // ?
			};
			
			var itemLabel = new Label {Text = "Channel:\n\nProperty:\n\nValue:\n\nComplete command:\n"};
			var copyCmdBtn = new Button {Text = "Revert value to here", Size = new Size(75,25)};
			
			var split = new Splitter
			{
				Orientation = Orientation.Horizontal,
				Position = this.Size.Width/2,
				SplitterWidth = 15,
				Panel1 = new Panel
				{
					Content = listBox
				},
				Panel2 = new Panel
				{
					Content = new StackLayout
					{
						Orientation = Orientation.Vertical,
						VerticalContentAlignment = VerticalAlignment.Stretch,
						Spacing = 10,
						Items =
						{
							new StackLayoutItem(itemLabel),
							new StackLayoutItem(copyCmdBtn)
						}
					}
				}
			};

			Content = new TableLayout
			{
				Rows =
				{
					new TableRow(split) { ScaleHeight = true},
					statusBar
				}
			};
			
			var timer = new UITimer()
			{
			    Interval = 0.001, // Interval is specified in seconds.
			};
			timer.Elapsed += (sender, e) =>
			{
				if (status.Text != "Ready!")
				{
					int index = FindStartIndex(currentLine, channels);
					if (index != -1)
					{
						if (index > prog.Value)
						{
							prog.Value = index;
						}
					}
				}
				if(currentLine.StartsWith(channels[channels.Length-1])) {status.Text = "Ready!";}
			    if(currentLine != prevLine && !currentLine.Contains("Start monitoring")) {
			        listBox.Items.Add(currentLine);
			        //listBox.SelectedKey = currentLine;
			        prevLine = currentLine;
			    }
			};
			timer.Start();
			
			this.SizeChanged += (sender,e) =>
			{
				split.Position = this.Size.Width/2;
			};
			
			listBox.SelectedIndexChanged += (sender, e) =>
			{
			    int selectedEntry = listBox.SelectedIndex;
				if (selectedEntry != -1) {
					string str = listBox.Items[selectedEntry].ToString();
					itemLabel.Text = $"Channel: {str.Split('>')[0]}\n\nProperty: {str.Split('>')[1].Split(' ')[1]}\n\nValue: {str.Split('>')[2]}\n\nComplete command:\n\nxfconf-query -c {str.Split('>')[0]} -p {str.Split('>')[1].Split(' ')[1]} -s {str.Split('>')[2]}";
				}
			};
			
			copyCmdBtn.Click += (sender, e) =>
			{
				if (itemLabel.Text != "Channel:\n\nProperty:\n\nValue:\n\nComplete command:\n")
				{
					string str = listBox.Items[listBox.SelectedIndex].ToString();
					Process.Start("bash", $"-c \"xfconf-query -c {str.Split('>')[0]} -p {str.Split('>')[1].Split(' ')[1]} -s {str.Split('>')[2]}\"");
				}
			};
			
			this.Closing += (sender, e) =>
			{
				// make sure there aren't any xfconf-query instances doing nothing but taking up resources
				Process.Start("bash", "-c \"killall xfconf-query\"");
			};
			
			var quitCommand = new Command { MenuText = "Quit", Shortcut = Application.Instance.CommonModifier | Keys.Q };
			quitCommand.Executed += (sender, e) => Application.Instance.Quit();
		}
	}
}