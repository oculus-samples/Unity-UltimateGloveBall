using Oculus.Avatar2;
using System.Collections;
using UnityEngine;

/* This class is an example of how to attach GameObjects to an avatar's critical joints. It retrieves all of a SampleAvatarEntity's
 * critical joints, and attache a cube primitive to each of them. As the avatar tracks body movement, the attached objects move with it.
 */
[RequireComponent(typeof(OvrAvatarEntity))]
public class SampleAvatarAttachments : MonoBehaviour
{
    private OvrAvatarEntity _avatarEnt;

    [SerializeField]
    private Vector3 AttachmentScale = new Vector3(0.1f, 0.1f, 0.1f);

    [SerializeField]
    private Color AttachmentColor = new Color(1.0f, 0.0f, 0.0f);

    protected IEnumerator Start()
    {
        _avatarEnt = GetComponent<OvrAvatarEntity>();
        yield return new WaitUntil(() => _avatarEnt.HasJoints);

        var criticalJoints = _avatarEnt.GetCriticalJoints();

        foreach (var jointType in criticalJoints)
        {
            Transform jointTransform = _avatarEnt.GetSkeletonTransform(jointType);

            if (!jointTransform)
            {
                OvrAvatarLog.LogError($"SampleAvatarAttachments: No joint transform found for {jointType} on {_avatarEnt.name} ");
                continue;
            }

            // Mirrored avatars have negative scale, which propagates down to the joints,
            // which makes the colliders sad because they're inside-out. Detect a negative
            // scale and apply its inverse to the collider's scale, to negate the negation
            // and turn the colliders right-side out.
            Vector3 fixSigns = jointTransform.lossyScale;
            fixSigns.x = Mathf.Sign(fixSigns.x);
            fixSigns.y = Mathf.Sign(fixSigns.y);
            fixSigns.z = Mathf.Sign(fixSigns.z);
            var attachmentObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            attachmentObj.transform.localScale = Vector3.Scale(AttachmentScale, fixSigns);
            attachmentObj.GetComponent<Renderer>().material.color = AttachmentColor;
            attachmentObj.transform.SetParent(jointTransform, false);
        }
    }
}
