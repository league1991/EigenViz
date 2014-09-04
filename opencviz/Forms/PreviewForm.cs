// Copyright (c) 2013 Sergiu Dotenco
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Drawing;
using System.Windows.Forms;
using Caustic.Visualizers.Properties;

namespace Caustic.Visualizers.Forms {
    public partial class PreviewForm : Form {
        public PreviewForm()
        {
            InitializeComponent();
        }

        public Image Image
        {
            set
            {
                pictureBox.Image = value;

                if (value != null) {
                    ClientSize = new Size(value.Width,
                        value.Height + toolStrip1.Height + statusStrip1.Height);
                    Text = string.Format(Resources.FormTitleFormat, value.Width, value.Height);
                }
            }
        }

        public string FileName
        {
            set
            {
                saveFileDialog1.FileName = value;
            }
        }

        private void saveToolStripButton_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                pictureBox.Image.Save(saveFileDialog1.FileName);
        }

        private void copyToolStripButton_Click(object sender, EventArgs e)
        {
            Clipboard.SetImage(pictureBox.Image);
        }

        private void PreviewForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                Close();
        }
    }
}