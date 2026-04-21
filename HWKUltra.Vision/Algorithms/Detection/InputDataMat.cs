using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using HWKUltra.Core;
using HWKUltra.Vision.Algorithms.Focus;

namespace HWKUltra.Vision.Algorithms.Detection
{
    /// <summary>
    /// Disposable value-object that bundles a source Mat with slot metadata and
    /// computed sharpness. Used by GetSliderROIV2 batch pipelines.
    /// Split out of the legacy SliderRoiExtractor.cs (GetSliderROI.cs) for maintainability.
    /// </summary>
    public class InputDataMat
    {
        public InputDataMat(short index, short row, short col, int srcrows, int srccols, Mat src, double sharpness)
        {
            this.index = index;
            this.row = row;
            this.col = col;
            this.Sharpness = sharpness;
            this.srcCols = srccols;
            this.srcRows = srcrows;
            sliceimgWidth = 1280;
            sliceimgHeight = 1280;
            this.SRC = src;
        }
        private bool disposed = false;

        public short index { get; }
        public short row { get; }
        public short col { get; }

        public int srcRows { get; }

        public int srcCols { get; }

        public int sliceimgWidth { get; }

        public int sliceimgHeight { get; }

        public double Sharpness { get; }

        public Mat SRC { get; set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~InputDataMat()
        {
            Dispose(false);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            { return; }
            if (disposing)
            {
                // clean
                if (SRC != null)
                {
                    SRC.Dispose();
                    SRC = null;
                }
            }
            // clean
            disposed = true;
        }
    }
}
