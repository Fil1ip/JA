using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Management;

namespace Brightness
{
    public partial class Form : System.Windows.Forms.Form
    {
        private ImageOperator imageOperator;
        private int cores = 0;
        public Form()
        {
            foreach (var item in new System.Management.ManagementObjectSearcher("Select * from Win32_Processor").Get())
            {
                cores += int.Parse(item["NumberOfCores"].ToString());
            }

            imageOperator = new ImageOperator();
            InitializeComponent();
            trackBar1.Value = cores;
            trackBar1.Minimum = 1;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            string imageLocation = "";
            try
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Filter = "Images only. | *.jpg; *.jpeg; *.png; *.bmp;";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    imageLocation = dialog.FileName;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Wystąpił błąd", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            if (imageLocation != "")
            {
                imageOperator.image = Image.FromFile(imageLocation);
                beforePicture.Image = imageOperator.image;
                imageOperator.bitmapFromImage();
            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (beforePicture.Image != null)
            {
                Cursor.Current = Cursors.WaitCursor;
                imageOperator.createRGB();
                imageOperator.Brighten(trackBar2.Value, radioButton1.Checked, trackBar1.Value);
                imageOperator.AfterImageFromRGB();
                afterPicture.Image = imageOperator.afterImage;
                timeLabel.Text = imageOperator.time.ToString() + " ms";
                Cursor.Current = Cursors.Default;
            }
            else
            {
                MessageBox.Show("Brak zdjęcia", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        

        }


        private void button3_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "JPeg Image|*.jpg|Bitmap Image|*.bmp|PNG Image|*.png";
            saveFileDialog1.Title = "Zapisz obraz";
            saveFileDialog1.ShowDialog();

            if (saveFileDialog1.FileName != "")
            {
                // Saves the Image via a FileStream created by the OpenFile method.
                System.IO.FileStream fs =
                    (System.IO.FileStream)saveFileDialog1.OpenFile();
                // Saves the Image in the appropriate ImageFormat based upon the
                // File type selected in the dialog box.
                // NOTE that the FilterIndex property is one-based.
                switch (saveFileDialog1.FilterIndex)
                {
                    case 1:
                        afterPicture.Image.Save(fs,
                          System.Drawing.Imaging.ImageFormat.Jpeg);
                        break;

                    case 2:
                        afterPicture.Image.Save(fs,
                          System.Drawing.Imaging.ImageFormat.Bmp);
                        break;

                    case 3:
                        afterPicture.Image.Save(fs,
                          System.Drawing.Imaging.ImageFormat.Png);
                        break;
                }

                fs.Close();
            }

        }
    }
}