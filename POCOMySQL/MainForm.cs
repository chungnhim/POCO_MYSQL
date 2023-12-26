using MySql.Data.MySqlClient;
using Renci.SshNet.Security;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;

namespace POCOMySQL
{
    public partial class MainForm : Form
    {
        private static readonly string strConnection =
           ConfigurationManager.ConnectionStrings["DbContext"].ConnectionString;
        private List<String> Tablenames = new List<String>();
        public MainForm()
        {
            InitializeComponent();
        }

        private void cmdConnectDatabase_Click(object sender, EventArgs e)
        {

            using (MySqlConnection connection = new MySqlConnection(strConnection))
            {
                connection.Open();
                string query = "show tables";
                MySqlCommand command = new MySqlCommand(query, connection);
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Tablenames.Add(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(reader.GetString(0)));
                    }
                }
                cbTableName.DataSource = Tablenames;
                connection.Close();
            }

        }

        private void cmdGenerateClass_Click(object sender, EventArgs e)
        {
            using (MySqlConnection connection = new MySqlConnection(strConnection))
            {
                connection.Open();
                string tablename = cbTableName.SelectedValue.ToString().ToLower();
                if (string.IsNullOrEmpty(tablename))
                    tablename = "customers";
                tablename = string.Format("select * from {0}", tablename);
                txtClass.Text = connection.GenerateClass(tablename);
                connection.Close();
            }
            Clipboard.Clear();    //Clear if any old value is there in Clipboard        
            Clipboard.SetText(txtClass.Text); //Copy text to Clipboa
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            using (MySqlConnection connection = new MySqlConnection(strConnection))
            {
                connection.Open();
                string query = "show tables";
                MySqlCommand command = new MySqlCommand(query, connection);
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Tablenames.Add(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(reader.GetString(0)));
                    }
                }
                cbTableName.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                cbTableName.AutoCompleteSource = AutoCompleteSource.CustomSource;
                cbTableName.DataSource = Tablenames;
                connection.Close();
            }
            openFileDialog1.InitialDirectory = @"C:\";
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.Title = "Browse Text Files";
            openFileDialog1.Filter = "image files (*.jpg)|*.jpg|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.CheckFileExists = true;
            openFileDialog1.CheckPathExists = true;
        }
        public string ImageToBase64(string path)
        {
            using (System.Drawing.Image image = System.Drawing.Image.FromFile(path))
            {
                using (MemoryStream m = new MemoryStream())
                {
                    image.Save(m, image.RawFormat);
                    byte[] imageBytes = m.ToArray();
                    string base64String = Convert.ToBase64String(imageBytes);
                    return base64String;
                }
            }
        }
        public System.Drawing.Image Base64ToImage(string base64String)
        {
            byte[] imageBytes = Convert.FromBase64String(base64String);
            MemoryStream ms = new MemoryStream(imageBytes, 0, imageBytes.Length);
            ms.Write(imageBytes, 0, imageBytes.Length);
            System.Drawing.Image image = System.Drawing.Image.FromStream(ms, true);
            return image;
        }
        private void cbTableName_TextChanged(object sender, EventArgs e)
        {
            //HandleTextChanged();

        }
        private void HandleTextChanged()
        {
            var txt = cbTableName.Text;
            var list = from d in Tablenames
                       where d.ToUpper().StartsWith(cbTableName.Text.ToUpper())
                       select d;
            if (list.Count() > 0)
            {
                cbTableName.DataSource = list.ToList();
                var sText = cbTableName.Items[0].ToString();
                cbTableName.SelectionStart = txt.Length;
                cbTableName.SelectionLength = sText.Length - txt.Length;
                cbTableName.DroppedDown = true;
                return;
            }
            else
            {
                cbTableName.DroppedDown = false;
                cbTableName.SelectionStart = txt.Length;
            }
        }

        private void cbTableName_KeyUp(object sender, KeyEventArgs e)
        {
            //if (e.KeyCode == Keys.Back)
            //{
            //    int sStart = cbTableName.SelectionStart;
            //    if (sStart > 0)
            //    {
            //        sStart--;
            //        if (sStart == 0)
            //        {
            //            cbTableName.Text = "";
            //        }
            //        else
            //        {
            //            cbTableName.Text = cbTableName.Text.Substring(0, sStart);
            //        }
            //    }
            //    e.Handled = true;
            //}
        }

        private void cmdGenerateInsert_Click(object sender, EventArgs e)
        {
            using (MySqlConnection connection = new MySqlConnection(strConnection))
            {
                connection.Open();
                string tablename = cbTableName.SelectedValue.ToString().ToLower();
                if (string.IsNullOrEmpty(tablename))
                    tablename = "customers";
                tablename = string.Format("select * from {0}", tablename);
                txtClass.AppendText(connection.GenerateSqlInsert(tablename));
                txtClass.AppendText("\r\n\r\n");
                connection.Close();
            }
            Clipboard.Clear();    //Clear if any old value is there in Clipboard        
            Clipboard.SetText(txtClass.Text); //Copy text to Clipboa
        }

        private void cmdGenerateSQLUpdate_Click(object sender, EventArgs e)
        {
            using (MySqlConnection connection = new MySqlConnection(strConnection))
            {
                connection.Open();
                string tablename = cbTableName.SelectedValue.ToString().ToLower();
                if (string.IsNullOrEmpty(tablename))
                    tablename = "customers";
                tablename = string.Format("select * from {0}", tablename);
                txtClass.AppendText(connection.GenerateSqlUpdate(tablename));
                txtClass.AppendText("\r\n\r\n");
                connection.Close();
            }
            Clipboard.Clear();    //Clear if any old value is there in Clipboard        
            Clipboard.SetText(txtClass.Text); //Copy text to Clipboa
        }

        private void cmdGenerateClassModel_Click(object sender, EventArgs e)
        {
            using (MySqlConnection connection = new MySqlConnection(strConnection))
            {
                connection.Open();
                string tablename = cbTableName.SelectedValue.ToString().ToLower();
                if (string.IsNullOrEmpty(tablename))
                    tablename = "customers";
                tablename = string.Format("select * from {0}", tablename);
                txtClass.AppendText(connection.GenerateClassModel(tablename));
                txtClass.AppendText("\r\n\r\n");
                connection.Close();
            }
            Clipboard.Clear();    //Clear if any old value is there in Clipboard        
            Clipboard.SetText(txtClass.Text); //Copy text to Clipboa
        }

        private void cmdGenerateService_Click(object sender, EventArgs e)
        {

        }

        private void cmdGenerateMySQLCommand_Click(object sender, EventArgs e)
        {
            using (MySqlConnection connection = new MySqlConnection(strConnection))
            {
                connection.Open();
                string tablename = cbTableName.SelectedValue.ToString().ToLower();
                if (string.IsNullOrEmpty(tablename))
                    tablename = "customers";
                tablename = string.Format("select * from {0}", tablename);
                //txtClass.AppendText(connection.GenerateSqlInsert(tablename));
                txtClass.AppendText(connection.GenerateClassParameter(tablename));
                txtClass.AppendText("\r\n\r\n");
                connection.Close();
            }
            Clipboard.Clear();    //Clear if any old value is there in Clipboard        
            Clipboard.SetText(txtClass.Text); //Copy text to Clipboa
        }

        private void cmdConvertImageToBase64_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
            Application.DoEvents();
            txtClass.Text = ImageToBase64(openFileDialog1.FileName);
        }

        private void cmdResizeImage_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
            string filename = Path.GetFileName(openFileDialog1.FileName);
        }

        private void btnExportFile_Click(object sender, EventArgs e)
        {
            //Folder Dtos
            var tableSelect = "";


            using (MySqlConnection connection = new MySqlConnection(strConnection))
            {
                connection.Open();
                string tablename = cbTableName.SelectedValue.ToString().ToLower();
                if (string.IsNullOrEmpty(tablename))
                    tablename = "customers";

                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                tableSelect = textInfo.ToTitleCase(tablename);

                tablename = string.Format("select * from {0}", tablename);
                string dataBody = connection.GenerateClassDtoToFile(tablename);

                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append($"namespace {txtNameSpace.Text}.Dtos.{tableSelect}s");
                stringBuilder.Append(Environment.NewLine);
                stringBuilder.Append("{");
                stringBuilder.Append(Environment.NewLine);
                stringBuilder.Append(dataBody);
                stringBuilder.Append(Environment.NewLine);
                stringBuilder.Append("}");

                //
                //txtClass.Text = stringBuilder.ToString();
                string folder = "export//1.Dtos//" + tableSelect;
                if (!Directory.Exists(folder)) { Directory.CreateDirectory(folder); }

                //Save file

                string filePath = Path.Combine(folder, $"{tableSelect}Dto.cs");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)) { }
                using (StreamWriter sr = new StreamWriter(filePath, true))
                {
                    sr.WriteLine(stringBuilder.ToString());
                }
            }

            //Folder Models

            using (MySqlConnection connection = new MySqlConnection(strConnection))
            {
                connection.Open();
                string tablename = cbTableName.SelectedValue.ToString().ToLower();
                if (string.IsNullOrEmpty(tablename))
                    tablename = "customers";

                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                tableSelect = textInfo.ToTitleCase(tablename);

                tablename = string.Format("select * from {0}", tablename);
                string dataBody = connection.GenerateClassModelToFile(tablename);

                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append($"namespace {txtNameSpace.Text}.Models.{tableSelect}s");
                stringBuilder.Append(Environment.NewLine);
                stringBuilder.Append("{");
                stringBuilder.Append(Environment.NewLine);
                stringBuilder.Append(dataBody);
                stringBuilder.Append(Environment.NewLine);
                stringBuilder.Append("}");

                string folder = "export//2.Models//" + tableSelect;

                if (!Directory.Exists(folder)) { Directory.CreateDirectory(folder); }

                //Save file

                string filePath = Path.Combine(folder, $"{tableSelect}Model.cs");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)) { }
                using (StreamWriter sr = new StreamWriter(filePath, true))
                {
                    sr.WriteLine(stringBuilder.ToString());
                }
            }

            //Folder AutoMapperProfiles
            var nameCreateModel = $"Create{tableSelect}Model";
            var nameViewModel = $"View{tableSelect}Model";
            var nameEditModel = $"Edit{tableSelect}Model";

            StringBuilder stringBuilderAutoMap = new StringBuilder();

            stringBuilderAutoMap.AppendLine($"CreateMap<{tableSelect}Dto, {nameCreateModel}>();");
            stringBuilderAutoMap.AppendLine($"CreateMap<{nameCreateModel}, {tableSelect}Dto>();");

            stringBuilderAutoMap.AppendLine($"CreateMap<{tableSelect}Dto, {nameViewModel}>();");
            stringBuilderAutoMap.AppendLine($"CreateMap<{nameViewModel}, {tableSelect}Dto>();");

            stringBuilderAutoMap.AppendLine($"CreateMap<{tableSelect}Dto, {nameEditModel}>();");
            stringBuilderAutoMap.AppendLine($"CreateMap<{nameEditModel}, {tableSelect}Dto>();");

            string folderAutoMapperProfiles = "export//6.AutoMapperProfiles//" + tableSelect;

            if (!Directory.Exists(folderAutoMapperProfiles)) { Directory.CreateDirectory(folderAutoMapperProfiles); }

            //Save file

            string filePathAutoMapperProfiles = Path.Combine(folderAutoMapperProfiles, $"{tableSelect}AutoMapperProfiles.cs");
            if (File.Exists(filePathAutoMapperProfiles))
            {
                File.Delete(filePathAutoMapperProfiles);
            }
            using (FileStream fs = new FileStream(filePathAutoMapperProfiles, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)) { }
            using (StreamWriter sr = new StreamWriter(filePathAutoMapperProfiles, true))
            {
                sr.WriteLine(stringBuilderAutoMap.ToString());
            }

            //Folder Service
            using (MySqlConnection connection = new MySqlConnection(strConnection))
            {
                connection.Open();
                string tablename = cbTableName.SelectedValue.ToString().ToLower();
                if (string.IsNullOrEmpty(tablename))
                    tablename = "customers";

                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                tableSelect = textInfo.ToTitleCase(tablename);

                tablename = string.Format("select * from {0}", tablename);
                string dataBody = connection.GenerateClassServiceToFile(tablename);

                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine($"using Dapper;");
                stringBuilder.AppendLine($"using {txtNameSpace.Text}.Dtos.{tableSelect}s;");
                stringBuilder.AppendLine($"using {txtNameSpace.Text}.Models;");
                stringBuilder.AppendLine($"using {txtNameSpace.Text}.Models.{tableSelect}s;");
                stringBuilder.AppendLine($"using {txtNameSpace.Text}.Repositories;");

                stringBuilder.Append($"namespace {txtNameSpace.Text}.Services.{tableSelect}s");
                stringBuilder.Append(Environment.NewLine);
                stringBuilder.Append("{");
                stringBuilder.Append(Environment.NewLine);
                stringBuilder.Append(dataBody);
                stringBuilder.Append(Environment.NewLine);
                stringBuilder.Append("}");

                string folder = "export//3.Services//" + tableSelect;

                if (!Directory.Exists(folder)) { Directory.CreateDirectory(folder); }

                //Save file

                string filePath = Path.Combine(folder, $"{tableSelect}Services.cs");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)) { }
                using (StreamWriter sr = new StreamWriter(filePath, true))
                {
                    sr.WriteLine(stringBuilder.ToString());
                }
            }

            //Folder Dependence file program or startup

            StringBuilder stringDependence = new StringBuilder();
            stringDependence.AppendLine($" builder.Services.AddTransient<I{tableSelect}Service , {tableSelect}Service>();");

            string folderDependence = "export//4.Dependence//" + tableSelect;

            if (!Directory.Exists(folderDependence)) { Directory.CreateDirectory(folderDependence); }

            //Save file

            string fileDependence = Path.Combine(folderDependence, $"{tableSelect}Dependence.cs");
            if (File.Exists(fileDependence))
            {
                File.Delete(fileDependence);
            }
            using (FileStream fs = new FileStream(fileDependence, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)) { }
            using (StreamWriter sr = new StreamWriter(fileDependence, true))
            {
                sr.WriteLine(stringDependence.ToString());
            }

            //Folder Controller

            using (MySqlConnection connection = new MySqlConnection(strConnection))
            {
                connection.Open();
                string tablename = cbTableName.SelectedValue.ToString().ToLower();
                if (string.IsNullOrEmpty(tablename))
                    tablename = "customers";

                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                tableSelect = textInfo.ToTitleCase(tablename);

                tablename = string.Format("select * from {0}", tablename);
                string dataBody = connection.GenerateClassControllerToFile(tablename);

                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine($"using AutoMapper;");
                stringBuilder.AppendLine($"using System.Security.Claims;");
                stringBuilder.AppendLine($"using Microsoft.AspNetCore.Mvc;");
                stringBuilder.AppendLine($"using Microsoft.AspNetCore.Authorization;");

                stringBuilder.AppendLine($"using {txtNameSpace.Text}.Dtos.{tableSelect}s;");
                stringBuilder.AppendLine($"using {txtNameSpace.Text}.Models;");
                stringBuilder.AppendLine($"using {txtNameSpace.Text}.Models.{tableSelect}s;");
                stringBuilder.AppendLine($"using {txtNameSpace.Text}.Services.{tableSelect}s;");

                stringBuilder.Append($"namespace {txtNameSpace.Text}.Controllers");
                stringBuilder.Append(Environment.NewLine);
                stringBuilder.Append("{");
                stringBuilder.Append(Environment.NewLine);
                stringBuilder.Append(dataBody);
                stringBuilder.Append(Environment.NewLine);
                stringBuilder.Append("}");

                string folder = "export//5.Controller//" + tableSelect;

                if (!Directory.Exists(folder)) { Directory.CreateDirectory(folder); }

                //Save file

                string filePath = Path.Combine(folder, $"{tableSelect}Controllers.cs");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)) { }
                using (StreamWriter sr = new StreamWriter(filePath, true))
                {
                    sr.WriteLine(stringBuilder.ToString());
                }
            }
            //Folder View
            //Create
            using (MySqlConnection connection = new MySqlConnection(strConnection))
            {
                connection.Open();
                string tablename = cbTableName.SelectedValue.ToString().ToLower();
                if (string.IsNullOrEmpty(tablename))
                    tablename = "customers";

                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                tableSelect = textInfo.ToTitleCase(tablename);

                tablename = string.Format("select * from {0}", tablename);
                string dataBody = connection.GenerateClassCreateCSHtmlToFile(tablename);

                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine($"@model {txtNameSpace.Text}.Models.{tableSelect}s.Create{tableSelect}Model");

                stringBuilder.AppendLine(dataBody);

                string folder = "export//6.View//" + tableSelect;

                if (!Directory.Exists(folder)) { Directory.CreateDirectory(folder); }

                //Save file

                string filePath = Path.Combine(folder, $"Create{tableSelect}.cshtml");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)) { }
                using (StreamWriter sr = new StreamWriter(filePath, true))
                {
                    sr.WriteLine(stringBuilder.ToString());
                }
            }

            //Edit
            using (MySqlConnection connection = new MySqlConnection(strConnection))
            {
                connection.Open();
                string tablename = cbTableName.SelectedValue.ToString().ToLower();
                if (string.IsNullOrEmpty(tablename))
                    tablename = "customers";

                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                tableSelect = textInfo.ToTitleCase(tablename);

                tablename = string.Format("select * from {0}", tablename);
                string dataBody = connection.GenerateClassEditCSHtmlToFile(tablename);

                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine($"@model {txtNameSpace.Text}.Models.{tableSelect}s.Edit{tableSelect}Model");

                stringBuilder.AppendLine(dataBody);

                string folder = "export//6.View//" + tableSelect;

                if (!Directory.Exists(folder)) { Directory.CreateDirectory(folder); }

                //Save file

                string filePath = Path.Combine(folder, $"Edit{tableSelect}.cshtml");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)) { }
                using (StreamWriter sr = new StreamWriter(filePath, true))
                {
                    sr.WriteLine(stringBuilder.ToString());
                }
            }

            //View
            using (MySqlConnection connection = new MySqlConnection(strConnection))
            {
                connection.Open();
                string tablename = cbTableName.SelectedValue.ToString().ToLower();
                if (string.IsNullOrEmpty(tablename))
                    tablename = "customers";

                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                tableSelect = textInfo.ToTitleCase(tablename);

                tablename = string.Format("select * from {0}", tablename);
                string dataBody = connection.GenerateClassViewCSHtmlToFile(tablename);

                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine($"@model {txtNameSpace.Text}.Dtos.{tableSelect}s.{tableSelect}Dto");

                stringBuilder.AppendLine(dataBody);

                string folder = "export//6.View//" + tableSelect;

                if (!Directory.Exists(folder)) { Directory.CreateDirectory(folder); }

                //Save file

                string filePath = Path.Combine(folder, $"View{tableSelect}.cshtml");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)) { }
                using (StreamWriter sr = new StreamWriter(filePath, true))
                {
                    sr.WriteLine(stringBuilder.ToString());
                }
            }

            //index

            using (MySqlConnection connection = new MySqlConnection(strConnection))
            {
                connection.Open();
                string tablename = cbTableName.SelectedValue.ToString().ToLower();
                if (string.IsNullOrEmpty(tablename))
                    tablename = "customers";

                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                tableSelect = textInfo.ToTitleCase(tablename);

                tablename = string.Format("select * from {0}", tablename);
                string dataBody = connection.GenerateClassIndexCSHtmlToFile(tablename);

                StringBuilder stringBuilder = new StringBuilder();
                
                stringBuilder.AppendLine(dataBody);

                string folder = "export//6.View//" + tableSelect;

                if (!Directory.Exists(folder)) { Directory.CreateDirectory(folder); }

                //Save file

                string filePath = Path.Combine(folder, $"index.cshtml");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)) { }
                using (StreamWriter sr = new StreamWriter(filePath, true))
                {
                    sr.WriteLine(stringBuilder.ToString());
                }
            }

        }


    }
}
