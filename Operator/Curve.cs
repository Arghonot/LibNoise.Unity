using System.Collections.Generic;
using UnityEngine;
using Debug = System.Diagnostics.Debug;

namespace LibNoise.Operator
{
    /// <summary>
    /// Provides a noise module that maps the output value from a source module onto an
    /// arbitrary function curve. [OPERATOR]
    /// </summary>
    public class Curve : SerializableModuleBase
    {
        #region Fields
        private Shader _sphericalGPUShader = Shader.Find("Xnoise/Modifier/Curve");
        public Material _materialGPU;

        private Texture2D curve;
        public AnimationCurve mathematicalCurve;

        private readonly List<KeyValuePair<double, double>> _data = new List<KeyValuePair<double, double>>();

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of Curve.
        /// </summary>
        public Curve()
            : base(1)
        {
        }

        /// <summary>
        /// Initializes a new instance of Curve.
        /// </summary>
        /// <param name="input">The input module.</param>
        public Curve(SerializableModuleBase input)
            : base(1)
        {
            Modules[0] = input;
            _materialGPU = new Material(_sphericalGPUShader);
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
        public List<KeyValuePair<double, double>> ControlPoints
        {
            get { return _data; }
        }

        #endregion

        #region Methods

        public void SetCurve(Texture2D newcurve)
        {
            curve = newcurve;
        }

        /// <summary>
        /// Adds a control point to the curve.
        /// </summary>
        /// <param name="input">The curves input value.</param>
        /// <param name="output">The curves output value.</param>
        public void Add(double input, double output)
        {
            var kvp = new KeyValuePair<double, double>(input, output);
            if (!_data.Contains(kvp))
            {
                _data.Add(kvp);
            }
            _data.Sort(
                delegate(KeyValuePair<double, double> lhs, KeyValuePair<double, double> rhs)
                {
                    return lhs.Key.CompareTo(rhs.Key);
                });
        }

        /// <summary>
        /// Clears the control points.
        /// </summary>
        public void Clear()
        {
            _data.Clear();
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
        public override RenderTexture GetValueGPU(GPURenderingDatas renderingDatas)
        {
            RenderTexture src = Modules[0].GetValueGPU(renderingDatas);

            if (curve == null)
            {
                curve = UtilsFunctions.GetCurveAsTexture(mathematicalCurve);
            }

            _materialGPU.SetTexture("_Src", src);
            _materialGPU.SetTexture("_Gradient", curve);

            return GetImage(_materialGPU, renderingDatas.size);
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
            Debug.Assert(Modules[0] != null);
            Debug.Assert(ControlPointCount >= 4);
            double val = Modules[0].GetValueCPU(x, y, z);

            return mathematicalCurve.Evaluate((float)val);
            //var smv = Modules[0].GetValue(x, y, z);
            //int ip;
            //for (ip = 0; ip < _data.Count; ip++)
            //{
            //    if (smv < _data[ip].Key)
            //    {
            //        break;
            //    }
            //}
            //var i0 = Mathf.Clamp(ip - 2, 0, _data.Count - 1);
            //var i1 = Mathf.Clamp(ip - 1, 0, _data.Count - 1);
            //var i2 = Mathf.Clamp(ip, 0, _data.Count - 1);
            //var i3 = Mathf.Clamp(ip + 1, 0, _data.Count - 1);
            //if (i1 == i2)
            //{
            //    return _data[i1].Value;
            //}
            ////double ip0 = _data[i1].Value;
            ////double ip1 = _data[i2].Value;
            //var ip0 = _data[i1].Key;
            //var ip1 = _data[i2].Key;
            //var a = (smv - ip0) / (ip1 - ip0);
            //return Utils.InterpolateCubic(_data[i0].Value, _data[i1].Value, _data[i2].Value,
            //    _data[i3].Value, a);
        }

        #endregion
    }
}