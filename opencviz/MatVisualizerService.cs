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

namespace Caustic.Visualizers {
    public class MatVisualizerService : IVsCppDebugUIVisualizer, IMatVisualizerService {
        public int DisplayValue(uint ownerHwnd, uint visualizerId, IDebugProperty3 debugProperty)
        {
            var propertyInfo = new DEBUG_PROPERTY_INFO[1];
            var hr = debugProperty.GetPropertyInfo(enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_ALL, 10,
                10000, new IDebugReference2[] { }, 0, propertyInfo);
            
            Debug.Assert(hr == VSConstants.S_OK, "IDebugProperty3.GetPropertyInfo failed");
            
            var baseNode = propertyInfo[0];
            var suggestedFileName = baseNode.bstrName;

            if (baseNode.bstrType != null && baseNode.bstrType.Contains("cv::Mat_")) // cv::Mat_<>
                baseNode = GetChildPropertyAt(0, baseNode);

            var flagsInfo = GetChildPropertyAt(0, baseNode);
            var dimsInfo = GetChildPropertyAt(1, baseNode);
            var rowsInfo = GetChildPropertyAt(2, baseNode);
            var colsInfo = GetChildPropertyAt(3, baseNode);
            var datastartInfo = GetChildPropertyAt(6, baseNode);
            var dataendInfo = GetChildPropertyAt(7, baseNode);
            var strideInfo = GetChildPropertyAt(0,
                GetChildPropertyAt(0, GetChildPropertyAt(11, baseNode)));
            var elementSizeInfo = GetChildPropertyAt(1,
                GetChildPropertyAt(1, GetChildPropertyAt(11, baseNode)));

            var flags = int.Parse(flagsInfo.bstrValue, NumberStyles.Any,CultureInfo.InvariantCulture);
            var rows = int.Parse(rowsInfo.bstrValue, NumberStyles.Any, CultureInfo.InvariantCulture);
            var cols = int.Parse(colsInfo.bstrValue, NumberStyles.Any, CultureInfo.InvariantCulture);
            var dims = int.Parse(dimsInfo.bstrValue, NumberStyles.Any, CultureInfo.InvariantCulture);
            var datastart = ulong.Parse(datastartInfo.bstrValue.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var dataend = ulong.Parse(dataendInfo.bstrValue.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var stride = int.Parse(strideInfo.bstrValue, NumberStyles.Any, CultureInfo.InvariantCulture);
            var elementSize = int.Parse(elementSizeInfo.bstrValue, NumberStyles.Any, CultureInfo.InvariantCulture);
            
            IDebugMemoryContext2 memoryContext;
            hr = datastartInfo.pProperty.GetMemoryContext(out memoryContext);
            Debug.Assert(hr == VSConstants.S_OK, "IDebugProperty.GetMemoryContext failed");

            IDebugMemoryBytes2 memoryBytes;
            hr = datastartInfo.pProperty.GetMemoryBytes(out memoryBytes);
            Debug.Assert(hr == VSConstants.S_OK, "IDebugProperty.GetMemoryBytes failed");

            var size = (uint)(dataend - datastart);
            var data = new byte[size];
            uint read;
            uint unreadable = 0;

            hr = memoryBytes.ReadAt(memoryContext, size, data, out read, ref unreadable);
            if (hr != VSConstants.S_OK)
                MessageBox.Show("IDebugMemoryBytes.ReadAt failed");
            Debug.Assert(hr == VSConstants.S_OK, "IDebugMemoryBytes.ReadAt failed");

            var channels = (((flags & (511 << 3)) >> 3) + 1);
            MessageBox.Show("mat size" + size +
                            "rows " + rows +
                            "cols " + cols +
                            "stride " + stride +
                            "channels " + channels +
                            "elem size " + elementSize
                            );
            
            var image = CreateBitmapFromMat(data, rows, cols, stride, channels, elementSize, flags);

            var form = new PreviewForm {
                Parent = Control.FromHandle((IntPtr)ownerHwnd),
                Image = image,
                FileName = suggestedFileName
            };

            form.ShowDialog();

            return hr;
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