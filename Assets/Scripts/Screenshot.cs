using System;
using System.Collections;
using UnityEngine;

namespace DefaultNamespace
{
    public class Screenshot : MonoBehaviour
    {
        public GameObject _unitychan;
        public int width = 1024;
        public int height = 1024;

        private bool _canScreenshot;
        
        void Start()
        {
            Camera.main.transform.LookAt(_unitychan.transform);
            Camera.main.transform.position = new Vector3(0, 0.8f, -2);
            Camera.main.transform.rotation = Quaternion.Euler(0, 0, 0);
            //Screen.SetResolution(width, height, false);
        }

        private IEnumerator TakeScreenshot()
        {
            //ScreenCapture.CaptureScreenshot("/tmp/a.png");
            Camera.main.transform.position = new Vector3(0, 0.8f, -2);
            Camera.main.transform.rotation = Quaternion.Euler(0, 0, 0);
            yield return new WaitForEndOfFrame();

            var rt = new RenderTexture(width, height, 24);
            Camera.main.targetTexture = rt;
            RenderTexture.active = rt;
            Camera.main.Render();
            yield return new WaitForEndOfFrame();
            
            var screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
            var minX = (Screen.width - width) / 2.0f;
            var minY = (Screen.height - height) / 2.0f;
            screenshot.ReadPixels(new Rect(minX, minY, minX + width, minY + height), 0, 0);
            Camera.main.targetTexture = null;
            RenderTexture.active = null; // JC: added to avoid errors
            Destroy(rt);
            
            var bytes = screenshot.EncodeToPNG();
            var filename = ScreenShotName();
            System.IO.File.WriteAllBytes(filename, bytes);
            Debug.Log(string.Format("Took screenshot to: {0}", filename));
        }
        
        private static string ScreenShotName()
        {
            return string.Format("{0}/screenshots/screen_{1}.png",
                "/tmp",
                0);
        }

        private void Update()
        {
            _canScreenshot |= Input.GetKeyDown("k");
            if (_canScreenshot)
            {
                StartCoroutine(TakeScreenshot());
                _canScreenshot = false;
            }
        }
    }
}