using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Windows;
using Input = UnityEngine.Input;

namespace DefaultNamespace
{
    public class Screenshot : MonoBehaviour
    {
        public Vector3 DefaultPosition = new (0, 0.8f, -2.0f);
        public Quaternion DefaultRotation = Quaternion.Euler(0, 0, 0);
        public string OutputDir = "/tmp/screenshots/unitychan_t";
        
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

            if (!Directory.Exists(OutputDir))
            {
                Directory.CreateDirectory(OutputDir);
            }
        }

        private void SetDefaultCamera()
        {
            Camera.main.transform.position = DefaultPosition;
            Camera.main.transform.rotation = DefaultRotation;
        }

        private void MoveCamera(int rotationX = 0, int rotationZ = 0)
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
        
        private async UniTask TakeScreenshot(UniTaskCompletionSource ucs, int rotationX = 0, int rotationZ = 0)
        {
            MoveCamera(rotationX, rotationZ);
            await UniTask.WaitForEndOfFrame(this);

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
            var filename = $"{OutputDir}/{rotationX}_{rotationZ}.png";
            System.IO.File.WriteAllBytes(filename, bytes);
            Debug.Log(string.Format("Took screenshot to: {0}", filename));
        }

        private async UniTask TakeScreenshots()
        {
            var rotXs = new List<int> { 0, 20, 40, 60, -20, -40, -60 };
            foreach (var rotationX in rotXs)
            {
                var rotationZ = 0;
                while (rotationZ < 360)
                {
                    Debug.Log($"screenshot from camera angle of x: {rotationX}, z: {rotationZ}");
                    var ucs = new UniTaskCompletionSource();
                    TakeScreenshot(ucs, rotationX, rotationZ).Forget();
                    await ucs.Task;
                    rotationZ += 10;
                }
            }
            Debug.Log($"finished");
            _canScreenshot = true;
        }
        
        private void Update()
        {
            //Camera.main.transform.RotateAround(_unitychan.transform.position, Vector3.up, 20 * Time.deltaTime);
            if (_canScreenshot && Input.GetKeyDown("k"))
            {
                _canScreenshot = false;
                TakeScreenshots().Forget();
            }
        }
    }
}