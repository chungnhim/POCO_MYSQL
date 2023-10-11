using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

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
                string tablename = cbTableName.SelectedValue.ToString().ToLower() ;
                if (string.IsNullOrEmpty(tablename))
                    tablename = "customers";
                tablename = string.Format("select * from {0}", tablename);
                txtClass.Text =  connection.GenerateClass(tablename);
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
                txtClass.AppendText( connection.GenerateSqlInsert(tablename));
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
    }
}
