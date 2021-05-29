using System;
using System.Xml.Serialization;
using UnityEngine;
using Debug = System.Diagnostics.Debug;

namespace LibNoise
{

    #region Enumerations

    /// <summary>
    /// Defines a collection of quality modes.
    /// </summary>
    public enum QualityMode
    {
        Low,
        Medium,
        High,
    }

    #endregion

    /// <summary>
    /// Base class for noise modules.
    /// </summary>
    [System.Serializable]
    public class SerializableModuleBase : IDisposable
    {
        #region Fields

        private SerializableModuleBase[] _modules;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of Helpers.
        /// </summary>
        /// <param name="count">The number of source modules.</param>
        public SerializableModuleBase(int count)
        {
            if (count > 0)
            {
                _modules = new SerializableModuleBase[count];
            }
        }

        #endregion

        #region Indexers

        /// <summary>
        /// Gets or sets a source module by index.
        /// </summary>
        /// <param name="index">The index of the source module to aquire.</param>
        /// <returns>The requested source module.</returns>
        public virtual SerializableModuleBase this[int index]
        {
            get
            {
                Debug.Assert(_modules != null);
                Debug.Assert(_modules.Length > 0);
                if (index < 0 || index >= _modules.Length)
                {
                    throw new ArgumentOutOfRangeException("Index out of valid module range");
                }
                if (_modules[index] == null)
                {
                    throw new ArgumentNullException("Desired element is null");
                }
                return _modules[index];
            }
            set
            {
                Debug.Assert(_modules.Length > 0);
                if (index < 0 || index >= _modules.Length)
                {
                    throw new ArgumentOutOfRangeException("Index out of valid module range");
                }
                if (value == null)
                {
                    throw new ArgumentNullException("Value should not be null");
                }
                _modules[index] = value;
            }
        }

        #endregion

        #region Properties
        protected SerializableModuleBase[] Modules
        {
            get { return _modules; }
        }

        /// <summary>
        /// Gets the number of source modules required by this noise module.
        /// </summary>
        public int SourceModuleCount
        {
            get { return (_modules == null) ? 0 : _modules.Length; }
        }

        #endregion

        #region Methods
        public virtual RenderTexture GetSphericalValueGPU(Vector2 size)
        {
            return null;
        }

        protected RenderTexture GetImage(Material material, Vector2 size)
        {
            RenderTexture rdB = new RenderTexture((int)size.x, (int)size.y, 16);

            RenderTexture.active = rdB;
            Graphics.Blit(Texture2D.whiteTexture, rdB, material);

            return rdB;
        }

        /// <summary>
        /// Returns the output value for the given input coordinates.
        /// </summary>
        /// <param name="x">The input coordinate on the x-axis.</param>
        /// <param name="y">The input coordinate on the y-axis.</param>
        /// <param name="z">The input coordinate on the z-axis.</param>
        /// <returns>The resulting output value.</returns>
        public virtual double GetValue(double x, double y, double z)
        {
            return 1;
        }

        /// <summary>
        /// Returns the output value for the given input coordinates.
        /// </summary>
        /// <param name="coordinate">The input coordinate.</param>
        /// <returns>The resulting output value.</returns>
        public double GetValue(Vector3 coordinate)
        {
            return GetValue(coordinate.x, coordinate.y, coordinate.z);
        }

        /// <summary>
        /// Returns the output value for the given input coordinates.
        /// </summary>
        /// <param name="coordinate">The input coordinate.</param>
        /// <returns>The resulting output value.</returns>
        public double GetValue(ref Vector3 coordinate)
        {
            return GetValue(coordinate.x, coordinate.y, coordinate.z);
        }

        #endregion

        #region IDisposable Members

        [XmlIgnore]
#if !XBOX360 && !ZUNE
        [NonSerialized]
#endif
        private bool _disposed;

        /// <summary>
        /// Gets a value whether the object is disposed.
        /// </summary>
        public bool IsDisposed
        {
            get { return _disposed; }
        }

        /// <summary>
        /// Immediately releases the unmanaged resources used by this object.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = Disposing();
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Immediately releases the unmanaged resources used by this object.
        /// </summary>
        /// <returns>True if the object is completely disposed.</returns>
        protected virtual bool Disposing()
        {
            if (_modules != null)
            {
                for (var i = 0; i < _modules.Length; i++)
                {
                    _modules[i].Dispose();
                    _modules[i] = null;
                }
                _modules = null;
            }
            return true;
        }

        #endregion
    }
}