using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using XNoise;

namespace LibNoise.Operator
{
    /// <summary>
    /// Provides a noise module that maps the output value from a source module onto a
    /// terrace-forming curve. [OPERATOR]
    /// </summary>
    public class Terrace : SerializableModuleBase
    {
        #region Fields

        private readonly List<double> _data = new List<double>();
        private bool _inverted;
        public AnimationCurve curve;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of Terrace.
        /// </summary>
        public Terrace()
            : base(1)
        {
        }

        /// <summary>
        /// Initializes a new instance of Terrace.
        /// </summary>
        /// <param name="input">The input module.</param>
        public Terrace(SerializableModuleBase input)
            : base(1)
        {
            Modules[0] = input;
        }

        /// <summary>
        /// Initializes a new instance of Terrace.
        /// </summary>
        /// <param name="inverted">Indicates whether the terrace curve is inverted.</param>
        /// <param name="input">The input module.</param>
        public Terrace(bool inverted, SerializableModuleBase input)
            : base(1)
        {
            Modules[0] = input;
            IsInverted = inverted;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the number of control points.
        /// </summary>
        public int ControlPointCount
        {
            get { return _data.Count; }
        }

        /// <summary>
        /// Gets the list of control points.
        /// </summary>
        public List<double> ControlPoints
        {
            get { return _data; }
        }

        /// <summary>
        /// Gets or sets a value whether the terrace curve is inverted.
        /// </summary>
        public bool IsInverted
        {
            get { return _inverted; }
            set { _inverted = value; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds a control point to the curve.
        /// </summary>
        /// <param name="input">The curves input value.</param>
        public void Add(double input)
        {
            if (!_data.Contains(input))
            {
                _data.Add(input);
            }
            _data.Sort(delegate(double lhs, double rhs) { return lhs.CompareTo(rhs); });
        }

        /// <summary>
        /// Clears the control points.
        /// </summary>
        public void Clear()
        {
            _data.Clear();
        }

        /// <summary>
        /// Auto-generates a terrace curve.
        /// </summary>
        /// <param name="steps">The number of steps.</param>
        public void Generate(int steps)
        {
            if (steps < 2)
            {
                throw new ArgumentException("Need at least two steps");
            }
            Clear();
            var ts = 2.0 / (steps - 1.0);
            var cv = -1.0;
            for (var i = 0; i < steps; i++)
            {
                Add(cv);
                cv += ts;
            }
        }

        public static AnimationCurve CreateCircularTerraceCurve(List<double> ctrlPts, bool invert = false)
        {
            if (ctrlPts == null || ctrlPts.Count < 2)
                throw new System.ArgumentException("Need at least 2 control points");

            var curve = new AnimationCurve();

            for (int i = 0; i < ctrlPts.Count - 1; i++)
            {
                float x0 = (float)ctrlPts[i];
                float x1 = (float)ctrlPts[i + 1];
                float dx = x1 - x0;
                if (dx <= 0) continue;

                float y0 = invert ? x1 : x0;
                float y1 = invert ? x0 : x1;

                var k0 = new Keyframe(x0, y0, 0, 0);
                curve.AddKey(k0);

                float xm = x0 + dx / 2f;
                float ym = (y0 + y1) / 2f;

                float arcSlope = (y1 - y0) / (dx * 0.5f); // Tangent as rise/run
                var km = new Keyframe(xm, ym, arcSlope, arcSlope);
                curve.AddKey(km);

                var k1 = new Keyframe(x1, y1, 0, 0);
                curve.AddKey(k1);
            }

            return curve;
        }

        #endregion

        #region ModuleBase Members

        /// <summary>
        /// Render this generator using a spherical shader.
        /// </summary>
        /// <param name="renderingDatas"></param>
        /// <returns>The generated image.</returns>
        /// 
        /// 
        public override RenderTexture GetValueGPU(GPUSurfaceNoise2d.GPURenderingDatas renderingDatas)
        {
            _materialGPU = XNoiseShaderCache.GetMaterial(XNoiseShaderPaths.Terrace);
            curve = CreateCircularTerraceCurve(_data, _inverted);
            var input = Modules[0].GetValueGPU(renderingDatas);

            _materialGPU.SetTexture("_Src", input);
            _materialGPU.SetTexture("_Gradient", UtilsFunctions.GetCurveAsTexture(curve));

            ImageFileHelpers.SaveToJPG(ImageFileHelpers.toTexture2D(input), "/", "TERRACE_INPUT");

            var res = GPUSurfaceNoise2d.GetImage(_materialGPU, renderingDatas);
            ImageFileHelpers.SaveToJPG(ImageFileHelpers.toTexture2D(res), "/", "TERRACE_OUTPUT");

            return res;
        }

        private void SaveRenderTexture(Texture2D tex)
        {
            UnityEngine.Debug.Log(Application.dataPath + "/" + "TerraceGradient" + ".png");

            File.WriteAllBytes(Application.dataPath + "/" + "TerraceGradient" + ".png", tex.EncodeToPNG());
        }

        /// <summary>
        /// Returns the output value for the given input coordinates.
        /// </summary>
        /// <param name="x">The input coordinate on the x-axis.</param>
        /// <param name="y">The input coordinate on the y-axis.</param>
        /// <param name="z">The input coordinate on the z-axis.</param>
        /// <returns>The resulting output value.</returns>
        public override double GetValueCPU(double x, double y, double z)
        {
            System.Diagnostics.Debug.Assert(Modules[0] != null);
            System.Diagnostics.Debug.Assert(ControlPointCount >= 2);

            curve = CreateCircularTerraceCurve(_data);
            var smv = Modules[0].GetValueCPU(x, y, z);

            return curve.Evaluate((float)smv);
        }

        #endregion
    }
}