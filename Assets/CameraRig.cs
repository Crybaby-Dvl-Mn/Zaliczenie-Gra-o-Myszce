void HandleCameraPosition()
{
    bool shouldSprintView = playerController != null && playerController.IsSprintActive;

    if (shouldSprintView && !isInSprintView)
    {
        transitionTimer += Time.deltaTime;
        if (transitionTimer >= transitionDelay)
            isInSprintView = true;
    }
    else if (!shouldSprintView)
    {
        transitionTimer = 0f;
        isInSprintView = false;
        hasArrivedAtAnchor = false;
    }

    // Pozycja TPS
    Vector3 tpsPos = player.position
        + Vector3.up * tpsHeight
        - Quaternion.Euler(0f, currentYaw, 0f) * Vector3.forward * tpsDistance;

    Vector3 targetPos = isInSprintView ? cameraSprintAnchor.position : tpsPos;

    // MoveTowards zamiast Lerp – gwarantuje dotarcie do celu
    float step = (isInSprintView ? transitionSpeed : tpsFollowSpeed) * Time.deltaTime;
    transform.position = Vector3.MoveTowards(transform.position, targetPos, step);

    // Sprawdzenie dotarcia
    if (isInSprintView)
    {
        float distToAnchor = Vector3.Distance(transform.position, cameraSprintAnchor.position);
        hasArrivedAtAnchor = distToAnchor <= transitionArriveThreshold;
    }

    // Rotacja
    if (isInSprintView)
    {
        transform.rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
    }
    else
    {
        Vector3 lookTarget = player.position + Vector3.up * tpsHeight;
        Quaternion lookRot = Quaternion.LookRotation(lookTarget - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, 8f * Time.deltaTime);
    }
}