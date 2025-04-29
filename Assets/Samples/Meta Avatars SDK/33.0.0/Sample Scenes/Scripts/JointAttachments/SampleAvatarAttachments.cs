#nullable disable

using Oculus.Avatar2;
using UnityEngine;

/* This class is an example of how to attach GameObjects to an avatar's critical joints. It retrieves all of a SampleAvatarEntity's
 * critical joints, and attaches a cube primitive to each of them. As the avatar tracks body movement, the attached objects move with it.
 */
[RequireComponent(typeof(OvrAvatarEntity))]
public class SampleAvatarAttachments : MonoBehaviour
{

    private OvrAvatarEntity avatarEntity;

    private OvrAvatarSocketDefinition hatSocket;
    private OvrAvatarSocketDefinition holsterSocket;
    private OvrAvatarSocketDefinition chestSocket;
    private OvrAvatarSocketDefinition swordBackSocket;

    public GameObject hat;
    public GameObject gun;
    public GameObject[] chestplates;
    public GameObject sword;

    private GameObject[] chestplateInstances = new GameObject[0];

    protected void Start()
    {
        avatarEntity = GetComponent<OvrAvatarEntity>();

        hatSocket = avatarEntity.CreateSocket(
           "HatSocket",
           CAPI.ovrAvatar2JointType.Head,
           height: 0.1f,
           width: 0.156f,
           depth: 0.209f,
           position: new Vector3(0.0866205f, 0.007995784f, 0),
           eulerAngles: new Vector3(0, 0, -13.163f),
           scaleGameObject: true
        );
        holsterSocket = avatarEntity.CreateSocket(
           "HolsterSocket",
           CAPI.ovrAvatar2JointType.Hips,
           position: new Vector3(0.049f, 0, -0.157f),
           eulerAngles: new Vector3(0, 0, 90)
        );
        chestSocket = avatarEntity.CreateSocket(
           "ChestSocket",
           CAPI.ovrAvatar2JointType.Chest,
           position: new Vector3(0f, 0, 0f),
           eulerAngles: new Vector3(0, 0, 0),
           width: 0.303f,
           depth: 0.239f,
           scaleGameObject: true
        );
        swordBackSocket = avatarEntity.CreateSocket(
           "SwordBackSocket",
           CAPI.ovrAvatar2JointType.Chest,
           position: new Vector3(0.065f, -0.25f, 0.097f),
           eulerAngles: new Vector3(0, -32, 0)
        );
        // Do not initialize sockets now, instead avatar Entity should
        // initialize then after avatar is loaded (and critical joints are known)

        chestplateInstances = new GameObject[chestplates.Length];
        for (var i = 0; i < chestplates.Length; i++)
        {
            chestplateInstances[i] = Instantiate(chestplates[i]);
            chestplateInstances[i].SetActive(false);
        }
    }

    protected void Update()
    {
        // Demonstrate stretching socketed items
        if (hatSocket != null && hatSocket.IsReady() && hatSocket.IsEmpty() && hat != null)
        {
            hatSocket.Attach(Instantiate(hat));
        }

        // Demonstrate scaling offset
        if (holsterSocket != null && holsterSocket.IsReady() && holsterSocket.IsEmpty() && gun != null)
        {
            holsterSocket.Attach(Instantiate(gun));
        }

        // Demonstrate dynamic t-shirt sizing
        if (chestSocket != null && chestSocket.IsReady() && chestplateInstances.Length >= 2)
        {
            var size = chestSocket.localScale.magnitude;
            if (size > 2f)
            {
                chestSocket.Attach(chestplateInstances[1]);
            }
            else
            {
                chestSocket.Attach(chestplateInstances[0]);
            }
        }

        if (swordBackSocket != null && swordBackSocket.IsReady() && swordBackSocket.IsEmpty() && sword != null)
        {
            swordBackSocket.Attach(Instantiate(sword));
        }
    }
}
