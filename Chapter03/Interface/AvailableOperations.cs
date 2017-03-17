namespace Chapter3.Interface
{
    /// <summary>
    /// Enumeration describing the available video/image operations
    /// </summary>
    public enum AvailableOperations
    {
        /// <summary>
        /// Operation to detect emotions
        /// </summary>
        Emotion,
        /// <summary>
        /// Operation to detect and track faces in videos
        /// </summary>
        FaceDetection,
        /// <summary>
        /// Operation to detect motion in videos
        /// </summary>
        MotionDetection,
        /// <summary>
        /// Operation to stabilize shaky videos
        /// </summary>
        Stabilization,
        /// <summary>
        /// Operation to generate dynamic video thumbnail
        /// </summary>
        Thumbnail
    }
}
