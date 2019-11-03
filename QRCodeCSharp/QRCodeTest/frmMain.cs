using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using QRCode;

namespace QRCodeTest
{
	public partial class frmMain : Form
	{
		public frmMain()
		{
			InitializeComponent();
		}

        public static Bitmap Encode(int scale, byte[] content)
        {
            bool[][] matrix = QRCodeEncoder.ComputeQRCode(content);

            var brush = new SolidBrush(Color.White);
            var image = new Bitmap(matrix.Length * scale, matrix.Length * scale);
            var g = Graphics.FromImage(image);

            g.FillRectangle(brush, new Rectangle(0, 0, image.Width, image.Height));

            brush.Color = Color.Black;

            for (int i = 0; i < matrix.Length; i++)
            {
                for (int j = 0; j < matrix.Length; j++)
                {
                    if (matrix[j][i])
                        g.FillRectangle(brush, j * scale, i * scale, scale, scale);
                }
            }

            return image;
        }

        private void btnEncode_Click(object sender, EventArgs e)
		{
			if (txtEncodeData.Text.Trim().Length == 0)
			{
				MessageBox.Show("Data must not be empty.");
				return;
			}

			if (ushort.TryParse(txtSize.Text, out ushort scale) == false || scale > 20)
			{
				MessageBox.Show("Invalid size!");
				return;
			}

			byte[] buffer = Encoding.UTF8.GetBytes(txtEncodeData.Text);

			picEncode.Image = Encode(scale, buffer);
		}

        private void txtEncodeData_TextChanged(object sender, EventArgs e)
        {
            if (txtEncodeData.Text.Trim().Length == 0)
            {
                picEncode.Image = null;
                return;
            }

            if (ushort.TryParse(txtSize.Text, out ushort scale) == false || scale > 20)
            {
                picEncode.Image = null;
                return;
            }

            byte[] buffer = Encoding.UTF8.GetBytes(txtEncodeData.Text);

            picEncode.Image = Encode(scale, buffer);
        }
    }
}
