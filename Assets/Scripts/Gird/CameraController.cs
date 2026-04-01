using UnityEngine;

//Reusable Camera Controller - to be used in the main gameplay scene and scene maker

public class CameraController : MonoBehaviour
{
    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float minZoom = 2f; 
    [SerializeField] private float maxZoom = 20f; 
    [SerializeField] private float zoomSmoothness = 10f;
    
    //Stuff to define camera bounds
    [Header("Bounds (Optional)")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private float minX = -10f;
    [SerializeField] private float maxX = 30f;
    [SerializeField] private float minY = -10f;
    [SerializeField] private float maxY = 30f;
    
    private Camera cam;
    private float targetZoom;
    
    //Need to use the awake method since it'll be used from the start of the scene
    void Awake()
    {
        //Getting the camera component and storing it
        //This bit was getting me confused for a while - need to make sure i remember to use it in the future if we use controls like this again
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = Camera.main;
        }
        
        if (cam != null)
        {
            //Setting initial target zoom to current zoom i.e. what the camera starts at
            targetZoom = cam.orthographicSize;
        }
    }
    
    //On every frame update, we handle the zooming and panning
    void Update()
    {
        //Returning early if no camera found
        if (cam == null) 
        {
            return;
        }
        
        HandleZoom();
        ApplyBounds();
    }
    
    //Handling the zooming of the camera - scrolling like in roll20
    private void HandleZoom()
    {
        //Setting up an input based on the axis of the mouse scroll wheel
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        
        //Making sure we only zoom if there's significant input - not for an accidental tiny scroll
        //Maybe remove this later
        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            //Setting up a vector variable to store the mouse position before zoom
            Vector3 mouseWorldPosBefore = cam.ScreenToWorldPoint(Input.mousePosition);
            
            //This bit figures out the zoom level based on input
            //Then we take the input * the speed from the target zoom we initialized before
            targetZoom -= scrollInput * zoomSpeed;
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
            
            //Moving the camera zoom smoothly
            //We use the camera's orthographic size to set the zoom level
            //We then set it to lerp between the current size and the target zoom for smoothness, stuff like this can be set up in the inspector
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.deltaTime * zoomSmoothness);
            
            //Actually adjusting the camera position to zoom towards the mouse position
            //Use a vector to figure out the position ofter zoom
            Vector3 mouseWorldPosAfter = cam.ScreenToWorldPoint(Input.mousePosition);
            //Then set up the difference (difference between before and after zoom) - this is how much we actually need to move the camera
            Vector3 diff = mouseWorldPosBefore - mouseWorldPosAfter;
            //Finally apply that difference to the camera position
            transform.position += diff;
        }
        else
        {
            //This bit just smoothly adjusts zoom if no scroll input - prevents jitter
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.deltaTime * zoomSmoothness);
        }
    }
    
    //Method for applying camera bounds - just helps to keep the camera within the grid area
    private void ApplyBounds()
    {
        if (!useBounds){
            return;
        }
        
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        transform.position = pos;
    }
    
    // === PUBLIC METHODS ===
    
    //Setting zoom directly
    public void SetZoom(float zoom)
    {
        targetZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
    }
    
    //Method to focus camera on a specific world position
    public void FocusOn(Vector3 worldPosition)
    {
        Vector3 newPos = worldPosition;
        newPos.z = transform.position.z;
        transform.position = newPos;
    }
    
    //Focusing on a specific tile position
    public void FocusOnTile(int x, int y)
    {
        FocusOn(new Vector3(x, y, 0));
    }
    
    //Resetting the camera view to center on the grid
    public void ResetView(int gridWidth, int gridHeight)
    {
        float centerX = gridWidth / 2f - 0.5f;
        float centerY = gridHeight / 2f - 0.5f;
        transform.position = new Vector3(centerX, centerY, -10f);
        
        // Set zoom to fit the grid
        targetZoom = Mathf.Max(gridWidth, gridHeight) / 2f + 2f;
        targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
    }
    
    //Zooming to fit a specific area
    public void ZoomToFit(float areaWidth, float areaHeight)
    {
        float screenRatio = (float)Screen.width / Screen.height;
        float targetRatio = areaWidth / areaHeight;
        
        if (screenRatio >= targetRatio)
        {
            targetZoom = areaHeight / 2f;
        }
        else
        {
            targetZoom = areaWidth / 2f / screenRatio;
        }
        
        targetZoom = Mathf.Clamp(targetZoom + 1f, minZoom, maxZoom);
    }
    
    //Setting camera bounds based on grid size
    public void SetBounds(int gridWidth, int gridHeight, float padding = 5f)
    {
        useBounds = true;
        minX = -padding;
        maxX = gridWidth + padding;
        minY = -padding;
        maxY = gridHeight + padding;
    }
}
