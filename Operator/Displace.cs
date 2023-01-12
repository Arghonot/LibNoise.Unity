using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace LibNoise.Operator
{
    /// <summary>
    /// Provides a noise module that uses three source modules to displace each
    /// coordinate of the input value before returning the output value from
    /// a source module. [OPERATOR]
    /// </summary>
    public class Displace : SerializableModuleBase
    {
        #region Constructors

        private Shader _sphericalGPUShader = Shader.Find("Xnoise/Transformers/Displace");
        private Material _materialGPU;
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
        public Displace(SerializableModuleBase input, SerializableModuleBase x, SerializableModuleBase y, SerializableModuleBase z)
            : base(4)
        {
            Modules[0] = input;
            Modules[1] = x;
            Modules[2] = y;
            Modules[3] = z;
        }

        #endregion

        #region Properties

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


        //private float getValue(RenderingAreaData area, Vector3 origin, int index, ProjectionType projection = ProjectionType.Flat)
        //{
        //    var rt = Modules[0].GetValueGPU(renderingDatas);
        //    Texture2D tex = new Texture2D(rt.width, rt.height);
        //    tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);

        //    UnityEngine.Debug.Log(tex.width + " " + tex.height);
        //    return (tex.GetPixel(0, 0).grayscale + 1f) / 2f;
        //}

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

            _materialGPU.SetTexture("_TextureA", Modules[1].GetValueGPU(renderingDatas));
            _materialGPU.SetTexture("_TextureB", Modules[2].GetValueGPU(renderingDatas));
            _materialGPU.SetTexture("_TextureC", Modules[3].GetValueGPU(renderingDatas));

            var tmpDisplacementMap = renderingDatas.displacementMap;
            renderingDatas.displacementMap = GetImage(_materialGPU, renderingDatas.size);
            var render = Modules[0].GetValueGPU(renderingDatas);
            renderingDatas.displacementMap = tmpDisplacementMap;

            return render;
        }

        private void SaveRenderTexture(RenderTexture renderedTexture)
        {
            var tex = new Texture2D(renderedTexture.width, renderedTexture.height);
            tex.ReadPixels(new Rect(0, 0, renderedTexture.width, renderedTexture.height), 0, 0);
            tex.Apply();

            UnityEngine.Debug.Log(Application.dataPath + "/" + "DisplacementMap" + ".png");

            File.WriteAllBytes(Application.dataPath + "/" + "DisplacementMap" + ".png", tex.EncodeToPNG());
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