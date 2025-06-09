using System;
using System.Diagnostics;
using UnityEngine;
using XNoise;

namespace LibNoise.Operator
{
    /// <summary>
    /// Provides a noise module that maps the output value from a source module onto an
    /// exponential curve. [OPERATOR]
    /// </summary>
    public class Exponent : SerializableModuleBase
    {
        #region Fields

        private double _exponent = 1.0;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of Exponent.
        /// </summary>
        public Exponent()
            : base(1)
        {
        }

        /// <summary>
        /// Initializes a new instance of Exponent.
        /// </summary>
        /// <param name="input">The input module.</param>
        public Exponent(SerializableModuleBase input)
            : base(1)
        {
            Modules[0] = input;
        }

        /// <summary>
        /// Initializes a new instance of Exponent.
        /// </summary>
        /// <param name="exponent">The exponent to use.</param>
        /// <param name="input">The input module.</param>
        public Exponent(double exponent, SerializableModuleBase input)
            : base(1)
        {
            Modules[0] = input;
            Value = exponent;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the exponent.
        /// </summary>
        public double Value
        {
            get { return _exponent; }
            set { _exponent = value; }
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
            _materialGPU = XNoiseShaderCache.GetMaterial(XNoiseShaderPaths.Exponent);

            _materialGPU.SetTexture("_TextureA", Modules[0].GetValueGPU(renderingDatas));
            _materialGPU.SetFloat("_Exponent", (float)_exponent);

            return GetImage(_materialGPU, renderingDatas);
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
            var v = Modules[0].GetValueCPU(x, y, z);
            return (Math.Pow(Math.Abs((v + 1.0) / 2.0), _exponent) * 2.0 - 1.0);
        }

        #endregion
    }
}