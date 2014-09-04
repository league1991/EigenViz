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
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Caustic.Visualizers.Forms;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Reflection;

namespace Caustic.Visualizers {
    public class MatVisualizerService : IVsCppDebugUIVisualizer, IMatVisualizerService {
        public int DisplayValue(uint ownerHwnd, uint visualizerId, IDebugProperty3 debugProperty)
        {
            var propertyInfo = new DEBUG_PROPERTY_INFO[1];
            var hr = debugProperty.GetPropertyInfo(enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_ALL, 10,
                10000, new IDebugReference2[] { }, 0, propertyInfo);
            
            Debug.Assert(hr == VSConstants.S_OK, "IDebugProperty3.GetPropertyInfo failed");
            
            // baseNode 代表Eigen::MatrixXd对象
            var baseNode = propertyInfo[0];
            var suggestedFileName = baseNode.bstrName;

            // 获得Eigen::MatrixXd对象的成员，GetChildPropertyAt后面的序号与成员声明的顺序一致

            if (baseNode.bstrType != null && baseNode.bstrType.Contains("Eigen::MatrixXd"))
                baseNode = GetChildPropertyAt(0, baseNode);

            // Eigen::Matrixd 结构: mat.m_storage.(m_data / m_rows / m_cols)
            var storageInfo = GetChildPropertyAt(1, baseNode);

            var dataInfo = GetChildPropertyAt(0, storageInfo);
            var rowInfo = GetChildPropertyAt(1, storageInfo);
            var colInfo = GetChildPropertyAt(2, storageInfo);

            // 获得 row col值
            uint rows = uint.Parse(rowInfo.bstrValue, NumberStyles.Any, CultureInfo.InvariantCulture);
            uint cols = uint.Parse(colInfo.bstrValue, NumberStyles.Any, CultureInfo.InvariantCulture);
            
            // 获得 data 内存数组
            IDebugMemoryContext2 memoryContext;
            hr = dataInfo.pProperty.GetMemoryContext(out memoryContext);
            Debug.Assert(hr == VSConstants.S_OK, "IDebugProperty.GetMemoryContext failed");

            IDebugMemoryBytes2 memoryBytes;
            hr = dataInfo.pProperty.GetMemoryBytes(out memoryBytes);
            Debug.Assert(hr == VSConstants.S_OK, "IDebugProperty.GetMemoryBytes failed");

            uint size = rows * cols * sizeof(double);
            byte[] buf = new byte[size];
            uint read;
            uint unreadable = 0;

            hr = memoryBytes.ReadAt(memoryContext, size, buf, out read, ref unreadable);
            if (hr != VSConstants.S_OK)
                MessageBox.Show("IDebugMemoryBytes.ReadAt failed");
            Debug.Assert(hr == VSConstants.S_OK, "IDebugMemoryBytes.ReadAt failed");
            
            //MessageBox.Show("rows " + rows + " cols " + cols);
            
            var image = CreateBitmapFromDoubleMat(buf, (int)rows, (int)cols);

            var form = new PreviewForm {
                Parent = Control.FromHandle((IntPtr)ownerHwnd),
                Image = image,
                FileName = suggestedFileName
            };

            form.ShowDialog();

            return hr;
        }

        private static Bitmap CreateBitmapFromDoubleMat(byte[] data, int rows, int cols)
        {
            Assembly myAssembly = Assembly.GetExecutingAssembly();
            string imgName = "";
            foreach (string name in myAssembly.GetManifestResourceNames())
            {
                if (name.ToLower().EndsWith("colormap.png"))
                    imgName = name;
            }

            System.IO.Stream myStream = myAssembly.GetManifestResourceStream(imgName);
            Bitmap colorMap = new Bitmap(myStream);
            int colorMapHeight = colorMap.Height-1;

            Bitmap image = new Bitmap(cols, rows, PixelFormat.Format32bppArgb);
            double minVal = double.MaxValue;
            double maxVal = double.MinValue;

            for (int c = 0, idx = 0; c < cols; ++c)
            {
                for (int r = 0; r < rows; ++r, idx += 8)
                {
                    double val = BitConverter.ToDouble(data, idx);
                    minVal = Math.Min(val, minVal);
                    maxVal = Math.Max(val, maxVal);
                }
            }

            double factor = (minVal < maxVal) ? 1.0 / (maxVal - minVal) : 0;
            for (int c = 0, idx = 0; c < cols; ++c )
            {
                for (int r = 0; r < rows; ++r, idx += 8)
                {
                    double val = BitConverter.ToDouble(data, idx);
                    val = (val - minVal) * factor;
                    int pos = (int)(colorMapHeight * val);                    
                    image.SetPixel(c, r, colorMap.GetPixel(0, pos));

//                     int tmp = (int)(val * 255);
//                     image.SetPixel(c, r, Color.FromArgb(255, tmp, tmp, tmp));
                }
            }
            return image;
        }
        private static Bitmap CreateBitmapFromMat(byte[] data, int rows, int cols, int stride,
                                                  int channels, int elementSize, int flags)
        {
            Bitmap image = new Bitmap(rows, cols, PixelFormat.Format32bppArgb);
            if (channels == 1 && (flags & 7) == 0) {
                // 8UC1
                var newStride = cols * 3;
//                var rgb = new byte[rows * newStride];

                for (int x = 0; x < cols; ++x) {
                    for (int y = 0; y < rows; ++y) {
                        var tmp = data[y * stride + x * elementSize];
//                         rgb[y * newStride + x * 3] = tmp;
//                         rgb[y * newStride + x * 3 + 1] = tmp;
//                         rgb[y * newStride + x * 3 + 2] = tmp;
                        image.SetPixel(y, x, Color.FromArgb(255, tmp, tmp, tmp));
                    }
                }

                stride = newStride;
//               data = rgb;
            }

            // TODO: Add support short, int, float and double
            if (channels == 3 ) 
            {
                // Assume BGR: ensure RGB ordering by swapping the R and B channels
                for (var x = 0; x < cols; ++x) {
                    for (var y = 0; y < rows; ++y) {
//                         var tmp = data[y * stride + x * elementSize];
//                         data[y * stride + x * elementSize] = data[y * stride + x * elementSize + 2];
//                         data[y * stride + x * elementSize + 2] = tmp;

                        var r = data[y * stride + x * elementSize];
                        var g = data[y * stride + x * elementSize + 1];
                        var b = data[y * stride + x * elementSize + 2];
                        image.SetPixel(y, x, Color.FromArgb(255, r,g,b));
                    }
                }
            }
            /*
            var pinnedArray = GCHandle.Alloc(data, GCHandleType.Pinned);

            try {
                var pointer = pinnedArray.AddrOfPinnedObject();
                return new Bitmap(cols, rows, stride, PixelFormat.Format24bppRgb, pointer);
            }
            finally {
                pinnedArray.Free();
            }*/
            return image;
        }

        private static DEBUG_PROPERTY_INFO GetChildPropertyAt(int index,
                                                              DEBUG_PROPERTY_INFO debugPropertyInfo)
        {
            var childInfo = new DEBUG_PROPERTY_INFO[1];
            IEnumDebugPropertyInfo2 enumDebugPropertyInfo;
            var guid = Guid.Empty;

            var hr =
                debugPropertyInfo.pProperty.EnumChildren(
                    enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE |
                    enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_PROP |
                    enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE_RAW, 10, ref guid,
                    enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_CHILD_ALL, null, 10000,
                    out enumDebugPropertyInfo);

            Debug.Assert(hr == VSConstants.S_OK, "GetChildPropertyAt: EnumChildren failed");

            if (enumDebugPropertyInfo != null) {
                uint childCount;
                hr = enumDebugPropertyInfo.GetCount(out childCount);
                Debug.Assert(hr == VSConstants.S_OK,
                    "GetChildPropertyAt: IEnumDebugPropertyInfo2.GetCount failed");
                Debug.Assert(childCount > index, "Given child index out of bounds");

                hr = enumDebugPropertyInfo.Skip((uint)index);
                Debug.Assert(hr == VSConstants.S_OK,
                    "GetChildPropertyAt: IEnumDebugPropertyInfo2.Skip failed");

                uint fetched;
                hr = enumDebugPropertyInfo.Next(1, childInfo, out fetched);
                Debug.Assert(hr == VSConstants.S_OK,
                    "GetChildPropertyAt: IEnumDebugPropertyInfo2.Next failed");
            }

            return childInfo[0];
        }
    }
}