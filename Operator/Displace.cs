using System.Diagnostics;
using System.IO;
using UnityEngine;
using XNoise;

namespace LibNoise.Operator
{
    /// <summary>
    /// Provides a noise module that uses three source modules to displace each
    /// coordinate of the input value before returning the output value from
    /// a source module. [OPERATOR]
    /// </summary>
    public class Displace : SerializableModuleBase
    {
        #region Fields

        private double _influence = 1.0;
        
        #endregion


        #region Constructors

        /// <summary>
        /// Initializes a new instance of Displace.
        /// </summary>
        public Displace()
            : base(4)
        {
        }

        /// <summary>
        /// Initializes a new instance of Displace.
        /// </summary>
        /// <param name="input">The input module.</param>
        /// <param name="x">The displacement module of the x-axis.</param>
        /// <param name="y">The displacement module of the y-axis.</param>
        /// <param name="z">The displacement module of the z-axis.</param>
        public Displace(SerializableModuleBase input, SerializableModuleBase x, SerializableModuleBase y, SerializableModuleBase z, double influence = 1f)
            : base(4)
        {
            Modules[0] = input;
            Modules[1] = x;
            Modules[2] = y;
            Modules[3] = z;
            _influence = influence;
        }

        #endregion

        #region Properties

        public double Influence
        {
            get { return _influence; }
            set { _influence = value; }
        }

        /// <summary>
        /// Gets or sets the controlling module on the x-axis.
        /// </summary>
        public SerializableModuleBase X
        {
            get { return Modules[1]; }
            set
            {
                System.Diagnostics.Debug.Assert(value != null);
                Modules[1] = value;
            }
        }

        /// <summary>
        /// Gets or sets the controlling module on the z-axis.
        /// </summary>
        public SerializableModuleBase Y
        {
            get { return Modules[2]; }
            set
            {
                System.Diagnostics.Debug.Assert(value != null);
                Modules[2] = value;
            }
        }

        /// <summary>
        /// Gets or sets the controlling module on the z-axis.
        /// </summary>
        public SerializableModuleBase Z
        {
            get { return Modules[3]; }
            set
            {
                System.Diagnostics.Debug.Assert(value != null);
                Modules[3] = value;
            }
        }

        #endregion

        #region ModuleBase Members
        public override RenderTexture GetValueGPU(GPURenderingDatas renderingDatas)
        {
            _materialGPU = XNoiseShaderCache.GetMaterial(XNoiseShaderPaths.Displace);

            var tmpDisplacementMap = renderingDatas.displacementMap;
            _materialGPU.SetTexture("_OriginalDisplacementMap", renderingDatas.displacementMap);
            _materialGPU.SetTexture("_TextureA", Modules[1].GetValueGPU(renderingDatas));
            _materialGPU.SetTexture("_TextureB", Modules[2].GetValueGPU(renderingDatas));
            _materialGPU.SetTexture("_TextureC", Modules[3].GetValueGPU(renderingDatas));
            _materialGPU.SetFloat("_Influence", (float)_influence);

            ImageFileHelpers.SaveToJPG(ImageFileHelpers.toTexture2D(renderingDatas.displacementMap), "/", "BEFORE");

            renderingDatas.displacementMap = GetImage(_materialGPU, renderingDatas);
            ImageFileHelpers.SaveToJPG(ImageFileHelpers.toTexture2D(renderingDatas.displacementMap), "/", "AFTER");
            var render = Modules[0].GetValueGPU(renderingDatas);
            renderingDatas.displacementMap = tmpDisplacementMap;

            return render;
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
            System.Diagnostics.Debug.Assert(Modules[3] != null);
            var dx = x + Modules[1].GetValueCPU(x, y, z);
            var dy = y + Modules[2].GetValueCPU(x, y, z);
            var dz = z + Modules[3].GetValueCPU(x, y, z);
            return Modules[0].GetValueCPU(dx, dy, dz);
        }

        #endregion
    }
}