using System.Text.RegularExpressions;
using System.IO;
using UnityEngine;
using Vuforia;
using System;

public class CameraImage : MonoBehaviour
{
    #region PRIVATE_VARS
    private PIXEL_FORMAT mPixelFormat = PIXEL_FORMAT.UNKNOWN_FORMAT;
    private bool mAccessCameraImage = true;
    private bool mFormatRegistered = false;
    private int pictureIndex = 0;
    private string pictureFileName = "/Users/AlisonNoyes/Desktop/vuforiapic";
    private string myKeySave = "byteArrayToXml";
    #endregion

    #region EXCEPTIONS
    public class QRCodeNotVisibleException : Exception
    {
        public QRCodeNotVisibleException()
        {
            Debug.LogError("QR codes must both be on screen to take a picture");
        }
    }
    #endregion

    #region MONOBEHAVIOUR_METHODS

    // Start is called before the first frame update
    void Start()
    {
        #if UNITY_EDITOR
        mPixelFormat = PIXEL_FORMAT.GRAYSCALE;
#else
        mPixelFormat = PIXEL_FORMAT.RGB888;
#endif

        VuforiaARController.Instance.RegisterVuforiaStartedCallback(OnVuforiaStarted);
        VuforiaARController.Instance.RegisterOnPauseCallback(OnPause);
    }

    #endregion // MONOBEHAVIOUR_METHODS

    #region AR_CONTROLLER_METHODS
    // Runs once camera is running
    private void OnVuforiaStarted()
    {
        if (CameraDevice.Instance.SetFrameFormat(mPixelFormat, true))
        {
            Debug.Log("Successfully registed pixel format: " + mPixelFormat.ToString());
            mFormatRegistered = true;
        }
        else
        {
            Debug.LogError("Failed to register pixel format: " + mPixelFormat.ToString());
            mFormatRegistered = false;
        }
    }

    // Runs when app is paused and resumed
    private void OnPause(bool paused)
    {
        if (paused)
        {
            Debug.Log("Game was paused\n");
            UnregisterFormat();
        }
        else
        {
            Debug.Log("Game was resumed\n");
            RegisterFormat();
        }
    }
    #endregion

    #region CAMERA_METHODS
    private void CaptureImage(Vector3 topLeftPosition, Vector3 bottomRightPosition)
    {
        if (mFormatRegistered)
        {
            if (mAccessCameraImage)
            {
                Image image = CameraDevice.Instance.GetCameraImage(mPixelFormat);

                if (image != null)
                {
                    Debug.Log(
                        "\nImage format: " + image.PixelFormat +
                        "\nImage size: " + image.Width + " by " + image.Height +
                        "\nBuffer size: " + image.BufferWidth + " by " + image.BufferHeight +
                        "\nImage stride: " + image.Stride + "\n"
                    );

                    byte[] pixels = image.Pixels;

                    if (pixels != null && pixels.Length > 0)
                    {
                        Debug.Log(
                            "\nImage pixels: " + pixels[0] + ", " + pixels[1] +
                            ", " + pixels[2] + "... \n"
                        );

                        Texture2D imageTexture = new Texture2D(image.Width, image.Height);
                        image.CopyToTexture(imageTexture);

                        // Save the image to the computer
                        SaveTextureAsPNG(imageTexture);

                        try
                        {
                            Texture2D croppedTexture = CropTexture(imageTexture, topLeftPosition, bottomRightPosition);
                            pictureFileName = "/Users/AlisonNoyes/Desktop/vuforiapiccrop";
                            SaveTextureAsPNG(croppedTexture);
                            pictureFileName = "/Users/AlisonNoyes/Desktop/vuforiapic";
                        }
                        catch (QRCodeNotVisibleException e)
                        {
                            throw new QRCodeNotVisibleException();
                        }
                    }
                }
            }
        }
    }

    private string AllStringToOneLine(string s)
    {
        string charReplacedSpace = " ";
        Regex regexp = new Regex("\\s+");
        return s = regexp.Replace(s, charReplacedSpace);
    }

    private void SaveTextureAsPNG(Texture2D toSave)
    {
        toSave = FlipTexture(toSave, true);
        byte[] png = toSave.EncodeToPNG();
        File.WriteAllBytes(pictureFileName + pictureIndex + ".png", png);
        pictureIndex++;
    }

    Texture2D CropTexture(Texture2D texture, Vector3 topLeft, Vector3 bottomRight)
    {
        Debug.Log("old dimensions: " + texture.width + " x " + texture.height);
        int newWidth = (int)(bottomRight.x - topLeft.x);
        int newHeight = (int)(topLeft.y - bottomRight.y);

        if (topLeft.x + newWidth > texture.width || bottomRight.y + newHeight > texture.height)
        {
            throw new QRCodeNotVisibleException();
        }

        Color[] c = texture.GetPixels((int)topLeft.x, (int)bottomRight.y, newWidth, newHeight);

        Texture2D cropped = new Texture2D(newWidth, newHeight);

        cropped.SetPixels(c);
        cropped.Apply();

        Debug.Log("cropped dimensions " + cropped.width + " x " + cropped.height);

        return cropped;
    }

    // Setting overXAxis to true flips over the x axis
    // Setting it to false flips over the y axis
    private Texture2D FlipTexture(Texture2D original, bool overXAxis)
    {
        Texture2D flipped = new Texture2D(original.width, original.height);

        int originalX = original.width;
        int originalY = original.height;

        for (int i = 0; i < originalX; i++)
        {
            for (int j = 0; j < originalY; j++)
            {
                if (overXAxis)
                {
                    flipped.SetPixel(i, originalY - j - 1, original.GetPixel(i, j));
                }
                else
                {
                    flipped.SetPixel(originalX - i - 1, j, original.GetPixel(i, j));
                }
            }
        }
        flipped.Apply();

        return flipped;
    }

    #endregion

    #region PIXEL_FORMAT_METHODS
    // Register pixel format
    void RegisterFormat()
    {
        if (CameraDevice.Instance.SetFrameFormat(mPixelFormat, true))
        {
            Debug.Log("Successfully registed pixel format: " + mPixelFormat.ToString());
            mFormatRegistered = true;
        }
        else
        {
            Debug.LogError("Failed to register pixel format: " + mPixelFormat.ToString());
            mFormatRegistered = false;
        }
    }

    // Unregister pixel format
    void UnregisterFormat()
    {
        Debug.Log("Unregistering pixel format: " + mPixelFormat.ToString());
        CameraDevice.Instance.SetFrameFormat(mPixelFormat, false);
        mFormatRegistered = false;
    }
    #endregion

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("space"))
        {
            GameObject topLeft = GameObject.FindGameObjectWithTag("top_left");
            GameObject bottomRight = GameObject.FindGameObjectWithTag("bottom_right");

            Debug.Log("topleft:" + topLeft.transform.position);
            Debug.Log("bottomright:" + bottomRight.transform.position);

            // Both QR codes are currently on the screen
            if (topLeft != null && bottomRight != null)
            {
                Vector3 topLeftWorldPosition = topLeft.transform.position;
                Vector3 bottomRightWorldPosition = bottomRight.transform.position;

                Camera arCamera = Camera.main;

                Vector3 topLeftPosition = arCamera.WorldToScreenPoint(topLeftWorldPosition);
                Vector3 bottomRightPosition = arCamera.WorldToScreenPoint(bottomRightWorldPosition);

                CaptureImage(topLeftPosition, bottomRightPosition);
            }
        }
    }
}
