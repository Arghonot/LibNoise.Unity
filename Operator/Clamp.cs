using System.Diagnostics;
using UnityEngine;
using XNoise;

namespace LibNoise.Operator
{
    /// <summary>
    /// Provides a noise module that clamps the output value from a source module to a
    /// range of values. [OPERATOR]
    /// </summary>
    public class Clamp : SerializableModuleBase
    {
        #region Fields

        private double _min = -1.0;
        private double _max = 1.0;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of Clamp.
        /// </summary>
        public Clamp()
            : base(1)
        {
        }

        /// <summary>
        /// Initializes a new instance of Clamp.
        /// </summary>
        /// <param name="input">The input module.</param>
        public Clamp(SerializableModuleBase input)
            : base(1)
        {
            Modules[0] = input;
        }

        /// <summary>
        /// Initializes a new instance of Clamp.
        /// </summary>
        /// <param name="input">The input module.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        public Clamp(double min, double max, SerializableModuleBase input)
            : base(1)
        {
            Minimum = min;
            Maximum = max;
            Modules[0] = input;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the maximum to clamp to.
        /// </summary>
        public double Maximum
        {
            get { return _max; }
            set { _max = value; }
        }

        /// <summary>
        /// Gets or sets the minimum to clamp to.
        /// </summary>
        public double Minimum
        {
            get { return _min; }
            set { _min = value; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Sets the bounds.
        /// </summary>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        public void SetBounds(double min, double max)
        {
            System.Diagnostics.Debug.Assert(min < max);
            _min = min;
            _max = max;
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
            _materialGPU = XNoiseShaderCache.GetMaterial(XNoiseShaderPaths.Clamp);

            _materialGPU.SetTexture("_TextureA", Modules[0].GetValueGPU(renderingDatas));
            _materialGPU.SetFloat("_Minimum", (float)_min);
            _materialGPU.SetFloat("_Maximum", (float)_max);

        return GPUSurfaceNoise2d.GetImage(_materialGPU, renderingDatas);
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
            if (_min > _max)
            {
                var t = _min;
                _min = _max;
                _max = t;
            }
            var v = Modules[0].GetValueCPU(x, y, z);
            if (v < _min)
            {
                return _min;
            }
            if (v > _max)
            {
                return _max;
            }
            return v;
        }

        #endregion
    }
}