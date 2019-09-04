using System.Text.RegularExpressions;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Vuforia;
using System;

public class CameraImage : MonoBehaviour
{
    public Material defaultMaterial;

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
    private void CaptureImage(Vector3 topLeftPosition, Vector3 bottomRightPosition, Vector3 bottomLeftPosition)
    {
        if (mFormatRegistered)
        {
            if (mAccessCameraImage)
            {
                Vuforia.Image image = CameraDevice.Instance.GetCameraImage(mPixelFormat);

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
                        pictureIndex--;

                        try
                        {
                            double theta = -Math.Atan((double)((bottomLeftPosition.x - topLeftPosition.x) / (topLeftPosition.y - bottomLeftPosition.y)));
                            Texture2D rotatedTexture = RotateTextureShear(imageTexture, theta);
                            pictureFileName = "/Users/AlisonNoyes/Desktop/vuforiapicrot";
                            SaveTextureAsPNG(rotatedTexture);
                            pictureIndex--;

                            // Adjust the locations of the QR codes in order to allow proper distortion and cropping
                            Vector3 center = new Vector3(imageTexture.width / 2, imageTexture.height / 2, 0);
                            bottomLeftPosition = rotateVectorAroundPoint(bottomLeftPosition, center, theta);
                            topLeftPosition = rotateVectorAroundPoint(topLeftPosition, center, theta);
                            bottomRightPosition = rotateVectorAroundPoint(bottomRightPosition, center, theta);
                            Debug.Log("top left corner: " + topLeftPosition.x + " " + topLeftPosition.y);
                            Debug.Log("bottom right corner: " + bottomRightPosition.x + " " + bottomRightPosition.y);

                            /*
                            Texture2D warpedTexture = WarpTexture(rotatedTexture, topLeftPosition, bottomLeftPosition, bottomRightPosition);
                            pictureFileName = "/Users/AlisonNoyes/Desktop/vuforiapicwarp";
                            SaveTextureAsPNG(warpedTexture);
                            pictureIndex--;
                            */                          

                            Texture2D croppedTexture = CropTexture(rotatedTexture, topLeftPosition, bottomRightPosition);
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

    private Vector3 rotateVectorAroundPoint(Vector3 vec, Vector3 point, double angle)
    {
        float xTrans = vec.x - point.x;
        float yTrans = vec.y - point.y;

        double u = xTrans * Math.Cos(angle) + yTrans * Math.Sin(angle) + point.x;
        double v = -xTrans * Math.Sin(angle) + yTrans * Math.Cos(angle) + point.y;

        return new Vector3((float)u, (float)v, vec.z);
    }
    #endregion

    #region TEXTURE_METHODS
    private void SaveTextureAsPNG(Texture2D toSave)
    {
        toSave = FlipTexture(toSave, true);
        byte[] png = toSave.EncodeToPNG();
        File.WriteAllBytes(pictureFileName + pictureIndex + ".png", png);
        pictureIndex++;
    }

    // Subtracts the second vector from the first
    private Vector3 vec3_subtract(Vector3 first, Vector3 second)
    {
        return new Vector3(first.x - second.x, first.y - second.y, first.z - second.z);
    }

    private Vector3 vec3_add(Vector3 first, Vector3 second)
    {
        return new Vector3(first.x + second.x, first.y + second.y, first.z + second.z);
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

    Texture2D RotateTexture(Texture2D original, double angle)
    {
        Texture2D rotated = new Texture2D(original.width, original.height);
        float xOrigin = original.width / 2;
        float yOrigin = original.height / 2;

        for (int x = 0; x < original.width; x++)
        {
            for (int y = 0; y < original.height; y++)
            {
                float xTrans = x - xOrigin;
                float yTrans = y - yOrigin;

                double u = xTrans * Math.Cos(angle) + yTrans * Math.Sin(angle) + xOrigin;
                double v = -xTrans * Math.Sin(angle) + yTrans * Math.Cos(angle) + yOrigin;

                //Debug.Log("x: " + x + "   y: " + y + "\n to \n u: " + u + "    v: " + v);
                if (u >= 0 && u < rotated.width && v >= 0 && v < rotated.height)
                {
                    rotated.SetPixel((int)u, (int)v, original.GetPixel(x, y));
                }
            }
        }

        rotated.Apply();
        return rotated;
    }

    Texture2D RotateTextureShear(Texture2D original, double angle)
    {
        float xOrigin = original.width / 2;
        float yOrigin = original.height / 2;

        angle = -angle;

        // First shear
        Texture2D rotated = VerticalShear(original, angle);

        // Second shear
        rotated = HorizontalShear(rotated, angle);

        // Third shear returns rotated image
        rotated = VerticalShear(rotated, angle);

        rotated.Apply();
        return rotated;
    }

    Texture2D VerticalShear(Texture2D texture, double angle)
    {
        Texture2D sheared = new Texture2D(texture.width, texture.height);
        float xOrigin = texture.width / 2;
        float yOrigin = texture.height / 2;

        for (int x = 0; x < texture.width; x++)
        {
            for (int y = 0; y < texture.height; y++)
            {
                float xTrans = x - xOrigin;
                float yTrans = y - yOrigin;

                double u = xTrans - yTrans * Math.Tan(angle / 2) + xOrigin;
                double v = y;

                if (0 <= u && u < texture.width && v >= 0 && v < texture.height)
                {
                    sheared.SetPixel((int)u, (int)v, texture.GetPixel(x, y));
                }
            }
        }

        sheared.Apply();
        return sheared;
    }

    Texture2D HorizontalShear(Texture2D texture, double angle)
    {
        Texture2D sheared = new Texture2D(texture.width, texture.height);
        float xOrigin = texture.width / 2;
        float yOrigin = texture.height / 2;

        for (int x = 0; x < texture.width; x++)
        {
            for (int y = 0; y < texture.height; y++)
            {
                float xTrans = x - xOrigin;
                float yTrans = y - yOrigin;

                double u = x;
                double v = xTrans * Math.Sin(angle) + yTrans + yOrigin;

                if (0 <= u && u < texture.width && v >= 0 && v < texture.height)
                {
                    sheared.SetPixel((int)u, (int)v, texture.GetPixel(x, y));
                }
            }
        }

        sheared.Apply();
        return sheared;
    }

    // Warps the given texture such that the three given locations are points on a rectangle
    Texture2D WarpTexture(Texture2D texture, Vector3 topLeft, Vector3 bottomLeft, Vector3 bottomRight)
    {
        Texture2D warped = new Texture2D(texture.width, texture.height);

        // Desired locations for the corners of the board
        Vector3 targetBottomLeft = new Vector3(topLeft.x, bottomRight.y, topLeft.z);
        Vector3 targetTopRight = new Vector3(bottomRight.x, topLeft.y, topLeft.z);

        float offset = targetBottomLeft.y - bottomLeft.y;
        Vector3 topRight = new Vector3(targetTopRight.x, targetTopRight.y - offset, targetTopRight.z);

        float halfHeight = (targetTopRight.y - targetBottomLeft.y) / 2;
        float topSlope = (topRight.y - topLeft.y) / (topRight.x - topLeft.x);
        float bottomSlope = (bottomRight.y - bottomLeft.y) / (bottomRight.x - bottomLeft.x);

        // Right side shrinks, left side stretches
        if (topSlope > 0)
        {
            for (int u = (int)topLeft.x; u < (int)topRight.x; u++)
            {
                float actual = halfHeight + topSlope * (u - topLeft.x);
                float scale = halfHeight / actual;

                // Warp top half
                for (int v = (int)(targetBottomLeft.y + halfHeight); v < (int)(topLeft.y); v++)
                {
                    float scaledPixelY = targetBottomLeft.y + halfHeight + (v - targetBottomLeft.y - halfHeight) / scale;
                    warped.SetPixel(u, v, texture.GetPixel(u, (int)scaledPixelY));
                }

                // Warp bottom half
                for (int v = (int)targetBottomLeft.y; v < (int)(targetBottomLeft.y + halfHeight); v++)
                {
                    float scaledPixelY = targetBottomLeft.y + halfHeight - (targetBottomLeft.y + halfHeight - v) * scale;
                    warped.SetPixel(u, v, texture.GetPixel(u, (int)scaledPixelY));
                }
            }
        }
        // Left side shrinks, right side stretches
        else if (topSlope < 0)
        {
            for (int u = (int)topLeft.x; u < (int)topRight.x; u++)
            {
                float actual = halfHeight + topSlope * (u - topLeft.x);
                float scale = halfHeight / actual;

                // Warp top half
                for (int v = (int)(targetBottomLeft.y + halfHeight); v < (int)(topLeft.y); v++)
                {
                    float scaledPixelY = targetBottomLeft.y + halfHeight + (v - targetBottomLeft.y - halfHeight) * scale;
                    warped.SetPixel(u, v, texture.GetPixel(u, (int)scaledPixelY));
                }

                // Warp bottom half
                for (int v = (int)targetBottomLeft.y; v < (int)(targetBottomLeft.y + halfHeight); v++)
                {
                    float scaledPixelY = targetBottomLeft.y + halfHeight - (targetBottomLeft.y + halfHeight - v) / scale;
                    warped.SetPixel(u, v, texture.GetPixel(u, (int)scaledPixelY));
                }
            }
        }
        // If slope is 0, no warping is needed

        warped.Apply();
        return warped;
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
        // Take picture of the board
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.tag == "take_picture")
                {
                    if (Input.GetKeyDown("space"))
                    {
                        GameObject topLeft = GameObject.FindGameObjectWithTag("top_left");
                        GameObject bottomLeft = GameObject.FindGameObjectWithTag("bottom_left");
                        GameObject bottomRight = GameObject.FindGameObjectWithTag("bottom_right");

                        Debug.Log("topleft:" + topLeft.transform.position);
                        Debug.Log("bottomright:" + bottomRight.transform.position);
                        Debug.Log("bottomleft:" + bottomLeft.transform.position);

                        // Both QR codes are currently on the screen
                        if (topLeft != null && bottomRight != null)
                        {
                            Vector3 topLeftWorldPosition = topLeft.transform.position;
                            Vector3 bottomRightWorldPosition = bottomRight.transform.position;
                            Vector3 bottomLeftWorldPosition = bottomLeft.transform.position;

                            Camera arCamera = Camera.main;

                            Vector3 topLeftPosition = arCamera.WorldToScreenPoint(topLeftWorldPosition);
                            Vector3 bottomRightPosition = arCamera.WorldToScreenPoint(bottomRightWorldPosition);
                            Vector3 bottomLeftPosition = arCamera.WorldToScreenPoint(bottomLeftWorldPosition);

                            CaptureImage(topLeftPosition, bottomRightPosition, bottomLeftPosition);
                        }
                    }
                }
            }
        }
    }
}
