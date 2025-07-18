﻿using UnityEngine;
using XNoise;

namespace LibNoise.Operator
{
    /// <summary>
    /// Provides a noise module that inverts the output value from a source module. [OPERATOR]
    /// </summary>
    public class Invert : SerializableModuleBase
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of Invert.
        /// </summary>
        public Invert()
            : base(1)
        {
        }

        /// <summary>
        /// Initializes a new instance of Invert.
        /// </summary>
        /// <param name="input">The input module.</param>
        public Invert(SerializableModuleBase input)
            : base(1)
        {
            Modules[0] = input;
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
            _materialGPU = XNoiseShaderCache.GetMaterial(XNoiseShaderPaths.Invert);

            _materialGPU.SetTexture("_TextureA", Modules[0].GetValueGPU(renderingDatas));

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
            return -Modules[0].GetValueCPU(x, y, z);
        }

        #endregion
    }
}