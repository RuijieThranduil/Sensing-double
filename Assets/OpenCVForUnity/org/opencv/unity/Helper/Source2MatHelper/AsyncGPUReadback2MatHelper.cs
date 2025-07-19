using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace OpenCVForUnity.UnityUtils.Helper
{
    /// <summary>
    /// A helper component class for efficiently converting Unity <c>Texture</c> objects, such as <c>RenderTexture</c> and external texture format <c>Texture2D</c>, to OpenCV <c>Mat</c> format using <c>AsyncGPUReadback</c>.
    /// </summary>
    /// <remarks>
    /// The <c>AsyncGPUReadback2MatHelper</c> class leverages <c>AsyncGPUReadback</c> to efficiently process texture data from Unity <c>Texture</c> classes that do not support direct pixel access through methods like <c>GetPixels</c>. 
    /// This includes <c>RenderTexture</c> and certain externally provided <c>Texture2D</c> formats. The class transfers the texture data asynchronously to the CPU and converts it to an OpenCV <c>Mat</c> object for further processing.
    /// 
    /// By utilizing <c>AsyncGPUReadback</c>, this component optimizes resource usage and reduces CPU load, making it ideal for real-time image processing applications that involve complex textures.
    /// 
    /// <strong>Note:</strong> By setting outputColorFormat to RGBA processing that does not include extra color conversion is performed.
    /// </remarks>
    /// <example>
    /// Attach this component to a GameObject and call <c>GetMat()</c> to retrieve the latest frame as a <c>Mat</c> object. 
    /// The helper class manages asynchronous readback operations and ensures efficient texture data conversion suitable for image processing in Unity.
    /// </example>
    public class AsyncGPUReadback2MatHelper : MonoBehaviour, ITextureSource2MatHelper
    {
        /// <summary>
        /// Set the source texture.
        /// </summary>
#if UNITY_EDITOR
        [OpenCVForUnityRuntimeDisable]
#endif
        [SerializeField, FormerlySerializedAs("sourceTexture"), TooltipAttribute("Set the source texture.")]
        protected Texture _sourceTexture;

        public virtual Texture sourceTexture
        {
            get { return _sourceTexture; }
            set
            {
                if (_sourceTexture != value)
                {
                    _sourceTexture = value;
                    if (hasInitDone)
                        Initialize(IsPlaying());
                    else if (isInitWaiting)
                        Initialize(autoPlayAfterInitialize);
                }
            }
        }

        /// <summary>
        /// Select the output color format.
        /// </summary>
#if UNITY_EDITOR
        [OpenCVForUnityRuntimeDisable]
#endif
        [SerializeField, FormerlySerializedAs("outputColorFormat"), TooltipAttribute("Select the output color format.")]
        protected Source2MatHelperColorFormat _outputColorFormat = Source2MatHelperColorFormat.RGBA;

        public virtual Source2MatHelperColorFormat outputColorFormat
        {
            get { return _outputColorFormat; }
            set
            {
                if (_outputColorFormat != value)
                {
                    _outputColorFormat = value;
                    if (hasInitDone)
                        Initialize(IsPlaying());
                    else if (isInitWaiting)
                        Initialize(autoPlayAfterInitialize);
                }
            }
        }

        /// <summary>
        /// The number of frames before the initialization process times out.
        /// </summary>
#if UNITY_EDITOR
        [OpenCVForUnityRuntimeDisable]
#endif
        [SerializeField, FormerlySerializedAs("timeoutFrameCount"), TooltipAttribute("The number of frames before the initialization process times out.")]
        protected int _timeoutFrameCount = 1500;

        public virtual int timeoutFrameCount
        {
            get { return _timeoutFrameCount; }
            set { _timeoutFrameCount = (int)Mathf.Clamp(value, 0f, float.MaxValue); }
        }

        /// <summary>
        /// UnityEvent that is triggered when this instance is initialized.
        /// </summary>
        [SerializeField, FormerlySerializedAs("onInitialized"), TooltipAttribute("UnityEvent that is triggered when this instance is initialized.")]
        protected UnityEvent _onInitialized;
        public UnityEvent onInitialized
        {
            get => _onInitialized;
            set => _onInitialized = value;
        }

        /// <summary>
        /// UnityEvent that is triggered when this instance is disposed.
        /// </summary>
        [SerializeField, FormerlySerializedAs("onDisposed"), TooltipAttribute("UnityEvent that is triggered when this instance is disposed.")]
        protected UnityEvent _onDisposed;
        public UnityEvent onDisposed
        {
            get => _onDisposed;
            set => _onDisposed = value;
        }

        /// <summary>
        /// UnityEvent that is triggered when this instance is error Occurred.
        /// </summary>
        [SerializeField, FormerlySerializedAs("onErrorOccurred"), TooltipAttribute("UnityEvent that is triggered when this instance is error Occurred.")]
        protected Source2MatHelperErrorUnityEvent _onErrorOccurred;
        public Source2MatHelperErrorUnityEvent onErrorOccurred
        {
            get => _onErrorOccurred;
            set => _onErrorOccurred = value;
        }

        protected bool didUpdateThisFrame = false;

        protected bool didUpdateImageBufferInCurrentFrame = false;

        /// <summary>
        /// The texture.
        /// </summary>
        protected Texture2D texture;

        /// <summary>
        /// The frame mat.
        /// </summary>
        protected Mat frameMat;

        /// <summary>
        /// The base mat.
        /// </summary>
        protected Mat baseMat;

        /// <summary>
        /// The useAsyncGPUReadback
        /// </summary>
        protected bool useAsyncGPUReadback;

        /// <summary>
        /// The base color format.
        /// </summary>
        protected Source2MatHelperColorFormat baseColorFormat = Source2MatHelperColorFormat.RGBA;

        /// <summary>
        /// Indicates whether this instance is waiting for initialization to complete.
        /// </summary>
        protected bool isInitWaiting = false;

        /// <summary>
        /// Indicates whether this instance has been initialized.
        /// </summary>
        protected bool hasInitDone = false;

        /// <summary>
        /// The initialization coroutine.
        /// </summary>
        protected IEnumerator initCoroutine;

        /// <summary>
        /// Indicates whether this instance has been initialized.
        /// </summary>
        protected bool isPlaying = false;

        /// <summary>
        /// If set to true play after completion of initialization.
        /// </summary>
        protected bool autoPlayAfterInitialize;


        // Update is called once per frame
        protected virtual void Update()
        {
            if (hasInitDone)
            {
                if (_sourceTexture == null || !isPlaying) return;

                if (_sourceTexture.width != baseMat.width() || _sourceTexture.height != baseMat.height())
                {

                    Debug.Log("AsyncGPUReadback2MatHelper:: " + "SOURCE_TEXTURE_SIZE_IS_CHANGED");

                    if (hasInitDone)
                        Initialize(IsPlaying());

                    return;
                }

                if (!useAsyncGPUReadback)
                {
                    Utils.textureToTexture2D(_sourceTexture, texture);

                    Utils.texture2DToMatRaw(texture, baseMat);

                    didUpdateThisFrame = true;
                    didUpdateImageBufferInCurrentFrame = true;
                }
                else
                {
                    AsyncGPUReadback.Request(_sourceTexture, 0, TextureFormat.RGBA32, (request) => { OnCompleteReadback(request); });
                }
            }
        }

        protected virtual void LateUpdate()
        {

            if (!hasInitDone)
                return;

            if (didUpdateThisFrame && !didUpdateImageBufferInCurrentFrame)
            {
                didUpdateThisFrame = false;
            }

            didUpdateImageBufferInCurrentFrame = false;

        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        protected virtual void OnDestroy()
        {
            Dispose();
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        /// <param name="autoPlay">If set to <c>true</c> play after completion of initialization.</param>
        public virtual void Initialize(bool autoPlay = true)
        {
#if !OPENCV_DONT_USE_UNSAFE_CODE
            useAsyncGPUReadback = SystemInfo.supportsAsyncGPUReadback;
#else
            useAsyncGPUReadback = false;
#endif

            //Debug.Log("AsyncGPUReadback2MatHelper:: " + "useAsyncGPUReadback: " + useAsyncGPUReadback);

            if (isInitWaiting)
            {
                CancelInitCoroutine();
                ReleaseResources();
            }

            autoPlayAfterInitialize = autoPlay;
            if (onInitialized == null)
                onInitialized = new UnityEvent();
            if (onDisposed == null)
                onDisposed = new UnityEvent();
            if (onErrorOccurred == null)
                onErrorOccurred = new Source2MatHelperErrorUnityEvent();

            initCoroutine = _Initialize();
            StartCoroutine(initCoroutine);
        }

        /// <summary>
        /// Initialize this instance.
        /// </summary>
        /// <param name="sourceTexture">Source Texture.</param>
        /// <param name="autoPlay">If set to <c>true</c> play after completion of initialization.</param>
        public virtual void Initialize(Texture sourceTexture, bool autoPlay = true)
        {

#if !OPENCV_DONT_USE_UNSAFE_CODE
            useAsyncGPUReadback = SystemInfo.supportsAsyncGPUReadback;
#else
            useAsyncGPUReadback = false;
#endif

            //Debug.Log("AsyncGPUReadback2MatHelper:: " + "useAsyncGPUReadback: " + useAsyncGPUReadback);

            if (isInitWaiting)
            {
                CancelInitCoroutine();
                ReleaseResources();
            }

            _sourceTexture = sourceTexture;
            autoPlayAfterInitialize = autoPlay;
            if (_onInitialized == null)
                _onInitialized = new UnityEvent();
            if (_onDisposed == null)
                _onDisposed = new UnityEvent();
            if (_onErrorOccurred == null)
                _onErrorOccurred = new Source2MatHelperErrorUnityEvent();

            initCoroutine = _Initialize();
            StartCoroutine(initCoroutine);
        }

        /// <summary>
        /// Initializes this instance by coroutine.
        /// </summary>
        protected virtual IEnumerator _Initialize()
        {
            if (hasInitDone)
            {
                ReleaseResources();

                if (onDisposed != null)
                    onDisposed.Invoke();
            }

            if (_sourceTexture == null)
            {
                isInitWaiting = false;
                initCoroutine = null;

                if (onErrorOccurred != null)
                    onErrorOccurred.Invoke(Source2MatHelperErrorCode.SOURCE_TEXTURE_IS_NULL, string.Empty);

                yield break;
            }

            isInitWaiting = true;

            // Wait one frame before starting initialization process
            yield return null;


//#if UNITY_2022_1_OR_NEWER
//            if (!SystemInfo.IsFormatSupported(_sourceTexture.graphicsFormat, GraphicsFormatUsage.ReadPixels))
//#else
            if (!SystemInfo.IsFormatSupported(_sourceTexture.graphicsFormat, FormatUsage.ReadPixels))
//#endif
            {
                Debug.Log("AsyncGPUReadback2MatHelper:: " + "The format of the set source texture is unsupported by AsyncGPUReadback, the conversion method has been changed to an inefficient method.");

                useAsyncGPUReadback = false;
            }


            int frameWidth = (int)_sourceTexture.width;
            int frameHeight = (int)_sourceTexture.height;

            if (!useAsyncGPUReadback)
            {
                texture = new Texture2D(frameWidth, frameHeight, TextureFormat.RGBA32, false);
            }
            baseMat = new Mat(frameHeight, frameWidth, CvType.CV_8UC4, new Scalar(0, 0, 0, 255));

            if (baseColorFormat == outputColorFormat)
            {
                frameMat = baseMat.clone();
            }
            else
            {
                frameMat = new Mat(baseMat.rows(), baseMat.cols(), CvType.CV_8UC(Source2MatHelperUtils.Channels(outputColorFormat)));
            }

            Debug.Log("AsyncGPUReadback2MatHelper:: " + " width:" + frameMat.width() + " height:" + frameMat.height() + " useAsyncGPUReadback:" + useAsyncGPUReadback);

            isInitWaiting = false;
            hasInitDone = true;
            initCoroutine = null;

            isPlaying = autoPlayAfterInitialize;

            if (onInitialized != null)
                onInitialized.Invoke();
        }

        void OnCompleteReadback(AsyncGPUReadbackRequest request)
        {
            //Debug.Log("AsyncGPUReadback2MatHelper:: " + "OnCompleteReadback");

            if (request.hasError)
            {
                Debug.Log("AsyncGPUReadback2MatHelper:: " + "GPU readback error detected. ");

            }
            else if (request.done)
            {
                //Debug.Log("AsyncGPUReadback2MatHelper:: " + "Start GPU readback done.);

                //Debug.Log("AsyncGPUReadback2MatHelper:: " + "Thread.CurrentThread.ManagedThreadId " + Thread.CurrentThread.ManagedThreadId);

#if !OPENCV_DONT_USE_UNSAFE_CODE
                MatUtils.copyToMat(request.GetData<byte>(), baseMat);
#endif

                Core.flip(baseMat, baseMat, 0);

                didUpdateThisFrame = true;
                didUpdateImageBufferInCurrentFrame = true;

                //Debug.Log("AsyncGPUReadback2MatHelper:: " + "End GPU readback done. ");
            }
        }

        /// <summary>
        /// Indicates whether this instance has been initialized.
        /// </summary>
        /// <returns><c>true</c>, if this instance has been initialized, <c>false</c> otherwise.</returns>
        public virtual bool IsInitialized()
        {
            return hasInitDone;
        }

        /// <summary>
        /// Starts the readback.
        /// </summary>
        public virtual void Play()
        {
            if (hasInitDone)
            {
                isPlaying = true;
            }
        }

        /// <summary>
        /// Pauses the readback.
        /// </summary>
        public virtual void Pause()
        {
            if (hasInitDone)
            {
                isPlaying = false;
            }
        }

        /// <summary>
        /// Stops the readback.
        /// </summary>
        public virtual void Stop()
        {
            if (hasInitDone)
            {
                isPlaying = false;
            }
        }

        /// <summary>
        /// Indicates whether the readback is currently playing.
        /// </summary>
        /// <returns><c>true</c>, if the readback is playing, <c>false</c> otherwise.</returns>
        public virtual bool IsPlaying()
        {
            return hasInitDone ? isPlaying : false;
        }

        /// <summary>
        /// Indicates whether the readback is paused.
        /// </summary>
        /// <returns><c>true</c>, if the readback is playing, <c>false</c> otherwise.</returns>
        public virtual bool IsPaused()
        {
            return hasInitDone ? isPlaying : false;
        }

        /// <summary>
        /// Return the active image device name.
        /// </summary>
        /// <returns>The active image device name.</returns>
        public virtual string GetDeviceName()
        {
            return "Unity_AsyncGPUReadback";
        }

        /// <summary>
        /// Returns the readback buffer width.
        /// </summary>
        /// <returns>The readback buffer width.</returns>
        public virtual int GetWidth()
        {
            if (!hasInitDone)
                return -1;
            return frameMat.width();
        }

        /// <summary>
        /// Returns the readback buffer height.
        /// </summary>
        /// <returns>The readback buffer height.</returns>
        public virtual int GetHeight()
        {
            if (!hasInitDone)
                return -1;
            return frameMat.height();
        }

        /// <summary>
        /// Returns the readback buffer base color format.
        /// </summary>
        /// <returns>The readback buffer base color format.</returns>
        public virtual Source2MatHelperColorFormat GetBaseColorFormat()
        {
            return baseColorFormat;
        }

        /// <summary>
        /// Returns the Source Texture.
        /// </summary>
        /// <returns>The Source Texture.</returns>
        public virtual Texture GetSourceTexture()
        {
            return hasInitDone ? _sourceTexture : null;
        }

        /// <summary>
        /// Indicates whether the readback buffer of the frame has been updated.
        /// </summary>
        /// <returns><c>true</c>, if the readback buffer has been updated <c>false</c> otherwise.</returns>
        public virtual bool DidUpdateThisFrame()
        {
            if (!hasInitDone)
                return false;

            return didUpdateThisFrame;
        }

        /// <summary>
        /// Get the mat of the current frame.
        /// </summary>
        /// <remarks>
        /// The Mat object's type is 'CV_8UC4' or 'CV_8UC3' or 'CV_8UC1' (ColorFormat is determined by the outputColorFormat setting).
        /// Please do not dispose of the returned mat as it will be reused.
        /// </remarks>
        /// <returns>The mat of the current frame.</returns>
        public virtual Mat GetMat()
        {
            if (!hasInitDone)
            {
                return frameMat;
            }

            didUpdateImageBufferInCurrentFrame = false;

            if (baseColorFormat == outputColorFormat)
            {
                baseMat.copyTo(frameMat);
            }
            else
            {
                Imgproc.cvtColor(baseMat, frameMat, Source2MatHelperUtils.ColorConversionCodes(baseColorFormat, outputColorFormat));
            }

            return frameMat;
        }

        /// <summary>
        /// Cancel Init Coroutine.
        /// </summary>
        protected virtual void CancelInitCoroutine()
        {

            if (initCoroutine != null)
            {
                StopCoroutine(initCoroutine);
                ((IDisposable)initCoroutine).Dispose();
                initCoroutine = null;
            }
        }

        /// <summary>
        /// To release the resources.
        /// </summary>
        protected virtual void ReleaseResources()
        {
            isInitWaiting = false;
            hasInitDone = false;

            didUpdateThisFrame = false;
            didUpdateImageBufferInCurrentFrame = false;

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }
            if (frameMat != null)
            {
                frameMat.Dispose();
                frameMat = null;
            }
            if (baseMat != null)
            {
                baseMat.Dispose();
                baseMat = null;
            }

        }

        /// <summary>
        /// Releases all resource used by the <see cref="AsyncGPUReadback2MatHelper"/> object.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="AsyncGPUReadback2MatHelper"/>. The
        /// <see cref="Dispose"/> method leaves the <see cref="AsyncGPUReadback2MatHelper"/> in an unusable state. After
        /// calling <see cref="Dispose"/>, you must release all references to the <see cref="AsyncGPUReadback2MatHelper"/> so
        /// the garbage collector can reclaim the memory that the <see cref="AsyncGPUReadback2MatHelper"/> was occupying.</remarks>
        public virtual void Dispose()
        {
            if (useAsyncGPUReadback)
            {
                AsyncGPUReadback.WaitAllRequests();
            }

            if (isInitWaiting)
            {
                CancelInitCoroutine();
                ReleaseResources();
            }
            else if (hasInitDone)
            {
                ReleaseResources();

                if (onDisposed != null)
                    onDisposed.Invoke();
            }
        }
    }
}