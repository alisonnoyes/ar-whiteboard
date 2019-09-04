# ar-whiteboard

On a button click, takes a picture of what is seen on the screen bounded by two QR codes and saves it to the desktop as a PNG.  The image is rotated, warped, and cropped to include only what is inside the QR code boundaries as a rectangular image.
Rotation is done with both a simple rotation method (which causes aliasing) and a method which shears the image horizontally and vertically in order to eliminate aliasing.

When the screen is clicked, an image from the desktop is projected over the board between the QR codes.

(Full Unity project is not included, just scripts that control saving and loading the images plus the main scene)
