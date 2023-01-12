using System.Diagnostics;
using UnityEngine;

namespace LibNoise.Operator
{
    /// <summary>
    /// Provides a noise module that outputs the product of the two output values from
    /// two source modules. [OPERATOR]
    /// </summary>
    public class Multiply : SerializableModuleBase
    {
        #region Fields

        private Shader _sphericalGPUShader = Shader.Find("Xnoise/Combiners/Multiply");
        private Material _materialGPU;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of Multiply.
        /// </summary>
        public Multiply()
            : base(2)
        {
        }

        /// <summary>
        /// Initializes a new instance of Multiply.
        /// </summary>
        /// <param name="lhs">The left hand input module.</param>
        /// <param name="rhs">The right hand input module.</param>
        public Multiply(SerializableModuleBase lhs, SerializableModuleBase rhs)
            : base(2)
        {
            Modules[0] = lhs;
            Modules[1] = rhs;
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
            _materialGPU = new Material(_sphericalGPUShader);

            _materialGPU.SetTexture("_TextureA", Modules[0].GetValueGPU(renderingDatas));
            _materialGPU.SetTexture("_TextureB", Modules[1].GetValueGPU(renderingDatas));

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
            System.Diagnostics.Debug.Assert(Modules[0] != null);
            System.Diagnostics.Debug.Assert(Modules[1] != null);
            return Modules[0].GetValueCPU(x, y, z) * Modules[1].GetValueCPU(x, y, z);
        }

        #endregion
    }
}