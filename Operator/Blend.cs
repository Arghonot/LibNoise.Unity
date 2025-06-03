using UnityEngine;
using Xnoise;

namespace LibNoise.Operator
{
    /// <summary>
    /// Provides a noise module that outputs a weighted blend of the output values from
    /// two source modules given the output value supplied by a control module. [OPERATOR]
    /// </summary>
    public class Blend : SerializableModuleBase
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of Blend.
        /// </summary>
        public Blend()
            : base(3)
        {
        }

        /// <summary>
        /// Initializes a new instance of Blend.
        /// </summary>
        /// <param name="lhs">The left hand input module.</param>
        /// <param name="rhs">The right hand input module.</param>
        /// <param name="controller">The controller of the operator.</param>
        public Blend(SerializableModuleBase lhs, SerializableModuleBase rhs, SerializableModuleBase controller)
            : base(3)
        {
            Modules[0] = lhs;
            Modules[1] = rhs;
            Modules[2] = controller;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the controlling module.
        /// </summary>
        public SerializableModuleBase Controller
        {
            get { return Modules[2]; }
            set
            {
                System.Diagnostics.Debug.Assert(value != null);
                Modules[2] = value;
            }
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
            _materialGPU = XNoiseShaderCache.GetMaterial(XNoiseShaderPaths.Blend);

            _materialGPU.SetTexture("_TextureA", Modules[0].GetValueGPU(renderingDatas));
            _materialGPU.SetTexture("_TextureB", Modules[1].GetValueGPU(renderingDatas));
            _materialGPU.SetTexture("_TextureC", Modules[2].GetValueGPU(renderingDatas));

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
            System.Diagnostics.Debug.Assert(Modules[1] != null);
            System.Diagnostics.Debug.Assert(Modules[2] != null);
            var a = Modules[0].GetValueCPU(x, y, z);
            var b = Modules[1].GetValueCPU(x, y, z);
            var c = (Modules[2].GetValueCPU(x, y, z) + 1.0) / 2.0;
            return Utils.InterpolateLinear(a, b, c);
        }

        #endregion
    }
}