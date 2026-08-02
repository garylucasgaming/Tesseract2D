using Engine.Core.Serialization;
using Engine.Editor.Theming;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using WinFormsApp1;

namespace Engine.Editor
{

  
    public partial class Tesseract2DLauncher : Form
    {
        // Reference to the panel or FlowLayoutPanel under your top header
        // Make sure your designer container for projects is named 'projectsFlowPanel' or adjust accordingly
        public Tesseract2DLauncher()
        {
            InitializeComponent();
            
            //ControlThemeExtensions.ApplySynthwaveTheme(this);

            this.Load += (s, e) => RefreshProjectListUI();
        }

        private void btnNewProject_Click(object sender, EventArgs e)
        {
            using(var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select the parent directory for your new game project.";
                folderDialog.ShowNewFolderButton = true;

                if(folderDialog.ShowDialog() == DialogResult.OK)
                {
                    var options = PromptForNewProjectDetails();
                    if(options == null)
                        return;

                    try
                    {
                        // 1. Create project folder structure
                        string projectRootPath = ProjectDirectoryFactory.CreateNewProject(folderDialog.SelectedPath, options.ProjectName);
                        
                        
                        // Define explicit directory paths to pass into the solution generator
                        string assetsPath = Path.Combine(projectRootPath, "Content", "Assets");
                        string scriptsPath = Path.Combine(assetsPath, "Scripts");

                        // 2. Generate custom .sln and .Gameplay.csproj with explicit directory injection
                        SolutionGenerator.GenerateUserSolution(projectRootPath, options.ProjectName, options.SelectedPlatforms, assetsPath, scriptsPath);
                       
                        // 3. Record in launcher history
                        ProjectHistoryStore.RecordProjectAccess(options.ProjectName, projectRootPath, options.SelectedPlatforms);

                        // 4. Launch Editor
                        OpenAndLaunchEditor(options.ProjectName, projectRootPath);
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show($"Failed to initialize project solution: {ex.Message}", "Project Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnOpenExisting_Click(object sender, EventArgs e)
        {
            using(var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select your existing project root folder.";
                folderDialog.ShowNewFolderButton = false;

                if(folderDialog.ShowDialog() == DialogResult.OK)
                {
                    string targetFolder = folderDialog.SelectedPath;
                    string projectName = Path.GetFileName(targetFolder);

                    OpenSelectedProject(new ProjectHistoryEntry
                    {
                        Name = projectName,
                        Path = targetFolder,
                        LastOpened = DateTime.Now
                    });
                }
            }
        }

        public void OpenSelectedProject(ProjectHistoryEntry entry)
        {
            string manifestPath = Path.Combine(entry.Path, "Content", "ProjectManifest.db");

            if(!File.Exists(manifestPath))
            {
                var result = MessageBox.Show(
                    $"Could not find 'Content/ProjectManifest.db' at:\n{entry.Path}\n\nWould you like to remove this project from your list?",
                    "Invalid Project Location",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );

                if(result == DialogResult.Yes)
                {
                    ProjectHistoryStore.RemoveFromHistory(entry.Path);
                    RefreshProjectListUI();
                }
                return;
            }

            // Read the freshest manifest state from disk
            var manifest = LoadManifest(manifestPath);
            var platforms = manifest?.TargetPlatforms ?? entry.TargetPlatforms;

            // Record access timestamp and launch editor
            ProjectHistoryStore.RecordProjectAccess(entry.Name, entry.Path, platforms);
            OpenAndLaunchEditor(entry.Name, entry.Path);
        }

        private void OpenAndLaunchEditor(string projectName, string projectRootPath)
        {
            // 1. Mount context inside EditorContextManager
            EditorContextManager.OpenProjectContext(projectRootPath);

            // 2. Instantiate and show Form1
            Form1 editorForm = new Form1();

            // Ensure that when Form1 closes, the launcher/app exits cleanly
            editorForm.FormClosed += (s, e) => Application.Exit();

            editorForm.Show();
            editorForm.OnProjectLoaded();

            // 3. Hide the Launcher form
            this.Hide();
        }

        /// <summary>
        /// Reads recent projects from JSON and dynamically generates selectable rows in the UI.
        /// </summary>
        public void RefreshProjectListUI()
        {
            // 💡 Search recursively through nested panels to find 'projectsFlowPanel'
            Control? container = this.Controls.Find("projectsFlowPanel", true).FirstOrDefault();

            // If you used a different name in the Designer (e.g. panel1, flowLayoutPanel1), 
            // you can also reference the variable directly if it exists in your form!
            if(container == null)
            {
                // Fallback: If you have a direct designer variable, e.g., 'this.projectsFlowPanel'
                // container = this.projectsFlowPanel; 
                return;
            }

            container.SuspendLayout();
            container.Controls.Clear();

            var recentProjects = ProjectHistoryStore.GetRecentProjects();

            foreach(var project in recentProjects)
            {
                Panel card = CreateProjectRowCard(project, container.ClientSize.Width - 25);
                container.Controls.Add(card);
            }

            container.ResumeLayout();
        }

        private Panel CreateProjectRowCard(ProjectHistoryEntry entry, int cardWidth)
        {
            Panel card = new Panel
            {
                Width = Math.Max(cardWidth, 300),
                Height = 80,
                BackColor = Color.Transparent,
                Margin = new Padding(5),
                Cursor = Cursors.Hand
            };

            Label titleLabel = new Label
            {
                Text = entry.Name,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(12, 8),
                AutoSize = true
            };

            int badgeX = 12;
            foreach(var platform in entry.TargetPlatforms)
            {
                Label badge = new Label
                {
                    Text = platform.ToUpper(),
                    Font = new Font("Segoe UI", 7f, FontStyle.Bold),
                    ForeColor = Color.Cyan,
                    BackColor = Color.FromArgb(20, 50, 60),
                    AutoSize = true,
                    Padding = new Padding(3, 1, 3, 1),
                    Location = new Point(badgeX, 50)
                };
                card.Controls.Add(badge);
                badgeX += badge.PreferredWidth + 6;
            }

            Label pathLabel = new Label
            {
                Text = entry.Path,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.DarkGray,
                Location = new Point(12, 32),
                AutoSize = true
            };

          

            Label dateLabel = new Label
            {
                Text = entry.LastOpened.ToShortDateString(),
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(card.Width - 110, 22),
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };

            // ❌ "X" Delete / Remove Button
            Button removeBtn = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.DarkGray,
                BackColor = Color.Transparent,
                Size = new Size(30, 30),
                Location = new Point(card.Width - 40, 15),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat
            };

            removeBtn.FlatAppearance.BorderSize = 0;
            removeBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(140, 40, 40); // Soft red background on hover
            removeBtn.FlatAppearance.MouseDownBackColor = Color.FromArgb(180, 20, 20);

            // Remove from history logic
            removeBtn.Click += (s, e) =>
            {
                var confirm = MessageBox.Show(
                    $"Remove '{entry.Name}' from your recent projects list?\n\n(This will not delete the files from your computer.)",
                    "Remove Project",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if(confirm == DialogResult.Yes)
                {
                    ProjectHistoryStore.RemoveFromHistory(entry.Path);
                    RefreshProjectListUI();
                }
            };

            // Card launch event
            EventHandler launchEvent = (s, e) => OpenSelectedProject(entry);
            card.Click += launchEvent;
            titleLabel.Click += launchEvent;
            pathLabel.Click += launchEvent;

            // Hover effects on card
            card.MouseEnter += (s, e) => card.BackColor = Color.FromArgb(42, 52, 52);
            card.MouseLeave += (s, e) => card.BackColor = Color.Transparent;

            card.Controls.Add(titleLabel);
            card.Controls.Add(pathLabel);
            card.Controls.Add(dateLabel);
            card.Controls.Add(removeBtn);

            return card;
        }
        private void AddExistingProjectButton_Click(object sender, EventArgs e)
        {
            using(var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select an existing project root directory.";
                folderDialog.ShowNewFolderButton = false;

                if(folderDialog.ShowDialog() == DialogResult.OK)
                {
                    string targetFolder = folderDialog.SelectedPath;
                    string manifestPath = Path.Combine(targetFolder, "Content", "ProjectManifest.db");

                    // 1. Verify and read ProjectManifest.db
                    if(!File.Exists(manifestPath))
                    {
                        MessageBox.Show(
                            $"The selected folder is not a valid project.\nCould not find 'Content/ProjectManifest.db' at:\n{targetFolder}",
                            "Invalid Project Folder",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );
                        return;
                    }

                    // 2. Read manifest data directly
                    var manifest = LoadManifest(manifestPath);
                    string projectName = string.IsNullOrEmpty(manifest?.ProjectName)
                        ? Path.GetFileName(targetFolder)
                        : manifest.ProjectName;

                    List<string> platforms = manifest?.TargetPlatforms ?? new List<string> { "Desktop" };

                    // 3. Register in history store
                    ProjectHistoryStore.RecordProjectAccess(projectName, targetFolder, platforms);

                    // 4. Refresh UI
                    RefreshProjectListUI();
                }
            }
        }

        public static ProjectManifest? LoadManifest(string projectRootPath)
        {
            string manifestPath = Path.Combine(projectRootPath, "Content", "ProjectManifest.db");

            if(!File.Exists(manifestPath))
                return null;

            try
            {
                string json = File.ReadAllText(manifestPath);
                return JsonSerializer.Deserialize<ProjectManifest>(json);
            }
            catch
            {
                return null;
            }
        }

        private NewProjectOptions? PromptForNewProjectDetails()
        {
            using(Form prompt = new Form
            {
                Width = 420,
                Height = 280,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Create New Tesseract2D Project",
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.FromArgb(28, 32, 32),
                ForeColor = Color.White
            })
            {
                Label nameLabel = new Label { Left = 20, Top = 15, Text = "Project Name:", AutoSize = true };
                TextBox nameBox = new TextBox { Left = 20, Top = 35, Width = 360 };

                Label platformLabel = new Label { Left = 20, Top = 75, Text = "Target Platforms:", AutoSize = true };

                CheckBox chkDesktop = new CheckBox { Text = "Desktop (Windows/Mac/Linux)", Left = 20, Top = 100, Checked = true, AutoSize = true };
                CheckBox chkAndroid = new CheckBox { Text = "Android", Left = 20, Top = 125, AutoSize = true };
                CheckBox chkiOS = new CheckBox { Text = "iOS", Left = 200, Top = 100, AutoSize = true };
                CheckBox chkWeb = new CheckBox { Text = "Web (WASM)", Left = 200, Top = 125, AutoSize = true };

                Button btnCreate = new Button
                {
                    Text = "Create Project",
                    Left = 260,
                    Top = 180,
                    Width = 120,
                    Height = 32,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(0, 122, 204),
                    DialogResult = DialogResult.OK
                };

                prompt.Controls.AddRange(new Control[] { nameLabel, nameBox, platformLabel, chkDesktop, chkAndroid, chkiOS, chkWeb, btnCreate });
                prompt.AcceptButton = btnCreate;

                if(prompt.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(nameBox.Text))
                {
                    var options = new NewProjectOptions { ProjectName = nameBox.Text.Trim() };
                    if(chkDesktop.Checked)
                        options.SelectedPlatforms.Add("Desktop");
                    if(chkAndroid.Checked)
                        options.SelectedPlatforms.Add("Android");
                    if(chkiOS.Checked)
                        options.SelectedPlatforms.Add("iOS");
                    if(chkWeb.Checked)
                        options.SelectedPlatforms.Add("Web");

                    return options;
                }

                return null;
            }
        }
        private string PromptUserForProjectName()
        {
            using(Form prompt = new Form
            {
                Width = 400,
                Height = 160,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "New Project",
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            })
            {
                Label textLabel = new Label { Left = 20, Top = 15, Text = "Project Name:", Width = 150 };
                TextBox textBox = new TextBox { Left = 20, Top = 40, Width = 340 };
                Button confirmation = new Button { Text = "Create", Left = 280, Width = 80, Top = 75, DialogResult = DialogResult.OK };

                prompt.Controls.Add(textBox);
                prompt.Controls.Add(confirmation);
                prompt.Controls.Add(textLabel);
                prompt.AcceptButton = confirmation;

                return prompt.ShowDialog() == DialogResult.OK ? textBox.Text.Trim() : string.Empty;
            }
        }
    }
}