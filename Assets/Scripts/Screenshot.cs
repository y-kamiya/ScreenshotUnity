using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Windows;
using Input = UnityEngine.Input;
using Directory = System.IO.Directory;

namespace DefaultNamespace
{
    public class Screenshot : MonoBehaviour
    {
        public Vector3 DefaultPosition = new (0, 0.75f, -1.5f);
        public List<int> RotationXs = new List<int>{0};
        // public List<int> RotationXs = new List<int>{ 0, 20, 40, 60, -20, -40, -60 };
        public int ViewCount = 64;
        public bool SequentialFileName = false;
        public Quaternion DefaultRotation = Quaternion.Euler(0, 0, 0);
        public string OutputDir = "output";
        
        public GameObject _unitychan;
        public int width = 1024;
        public int height = 1024;
        private float _baseDistance;
        private bool _canScreenshot = true;
        
        void Start()
        {
            _unitychan.transform.rotation = Quaternion.Euler(0, 180, 0);
            //Camera.main.transform.LookAt(_unitychan.transform);
            SetDefaultCamera();
            _baseDistance = Distance();
            //Screen.SetResolution(width, height, false);
        }

        private void SetDefaultCamera()
        {
            Camera.main.transform.position = DefaultPosition;
            Camera.main.transform.rotation = DefaultRotation;
        }

        private void MoveCamera(float rotationX = 0, float rotationZ = 0)
        {
            SetDefaultCamera();
            Camera.main.transform.RotateAround(_unitychan.transform.position, Vector3.right, rotationX);
            Camera.main.transform.RotateAround(_unitychan.transform.position, Vector3.up, rotationZ);
            Camera.main.transform.position = Vector3.MoveTowards(
                Camera.main.transform.position, _unitychan.transform.position, Distance() - _baseDistance);
        }

        private float Distance()
        {
            return (Camera.main.transform.position - _unitychan.transform.position).magnitude;
        }
        
        private async UniTask TakeScreenshot(UniTaskCompletionSource ucs, float rotationX = 0, float rotationZ = 0, bool moveCamera = true, string basename = "")
        {
            if (moveCamera)
            {
                MoveCamera(rotationX, rotationZ);
                await UniTask.WaitForEndOfFrame(this);
            }

            var rt = new RenderTexture(width, height, 24);
            Camera.main.targetTexture = rt;
            RenderTexture.active = rt;
            Camera.main.Render();
            await UniTask.WaitForEndOfFrame(this);
            
            var screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
            var minX = (Screen.width - width) / 2.0f;
            var minY = (Screen.height - height) / 2.0f;
            screenshot.ReadPixels(new Rect(minX, minY, minX + width, minY + height), 0, 0);
            ucs.TrySetResult();
            
            Camera.main.targetTexture = null;
            RenderTexture.active = null; // JC: added to avoid errors
            Destroy(rt);
            
            var bytes = screenshot.EncodeToPNG();
            if (string.IsNullOrEmpty(basename))
            {
                basename = $"{rotationX}_{rotationZ}.png";
            }
            var filepath = $"{OutputDir}/{basename}";
            System.IO.File.WriteAllBytes(filepath, bytes);
            Debug.Log(string.Format("Took screenshot to: {0}", filepath));
        }

        private async UniTask TakeScreenshots()
        {
            foreach (var rotationX in RotationXs)
            {
                var delta = 360.0f / ViewCount;
                for (var i = 0; i < ViewCount; i++)
                {
                    var rotationZ = i * delta;
                    var basename = SequentialFileName ? $"{i}.png" : $"{rotationX}_{rotationZ}.png";
                    Debug.Log($"screenshot from camera angle of x: {rotationX}, z: {rotationZ}");
                    var ucs = new UniTaskCompletionSource();
                    TakeScreenshot(ucs, rotationX, rotationZ, basename: basename).Forget();
                    await ucs.Task;
                }
            }
            Debug.Log($"finished");
            _canScreenshot = true;
        }
        
        private async UniTask TakeCurrentShot()
        {
            var ucs = new UniTaskCompletionSource();
            TakeScreenshot(ucs, 0, 0, false).Forget();
            await ucs.Task;
            _canScreenshot = true;
        }

        private void CreateOutputDir()
        {
            if (!Directory.Exists(OutputDir))
            {
                Directory.CreateDirectory(OutputDir);
                Debug.Log($"created directory of {OutputDir}");
            }
        }

        private void Update()
        {
            //Camera.main.transform.RotateAround(_unitychan.transform.position, Vector3.up, 20 * Time.deltaTime);
            if (_canScreenshot && Input.GetKeyDown("k"))
            {
                _canScreenshot = false;
                CreateOutputDir();
                TakeScreenshots().Forget();
            }

            if (_canScreenshot && Input.GetKeyDown("c"))
            {
                _canScreenshot = false;
                CreateOutputDir();
                TakeCurrentShot().Forget();
            }
        }
    }
}
