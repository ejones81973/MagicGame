using UnityEngine;
using UnityEngine.InputSystem;

namespace Spellright
{
    /// <summary>Graybox course builder for the isolated formation prototype.</summary>
    public sealed class OverworldTestScene : MonoBehaviour
    {
        [SerializeField] InputActionAsset inputActions;
        [SerializeField] OverworldMovementTuning tuning;
        OverworldPartyManager party;
        OverworldPuzzleDirector puzzles;
        Camera sceneCamera;
        string prompt = "Explore the course and try each formation.";
        GUIStyle titleStyle, bodyStyle, hintStyle;

        void Start()
        {
            if (!tuning) tuning = ScriptableObject.CreateInstance<OverworldMovementTuning>();
            BuildCourse();
            BuildCameraAndLight();
            var input = gameObject.AddComponent<OverworldInputReader>();
            input.Initialize(inputActions);
            party = gameObject.AddComponent<OverworldPartyManager>();
            party.Initialize(input, tuning, sceneCamera);
            puzzles = gameObject.AddComponent<OverworldPuzzleDirector>();
            ConfigurePuzzles();
            Debug.Log("Spellright overworld ready: party, camera, input, formations and puzzles initialized.");
        }

        void ConfigurePuzzles()
        {
            puzzles.Configure(party, spreadPlates, spreadDoor, relayStations, relayGates,
                carryPlate, heavyCube, mixedSpreadStations, mixedSteam, mixedStorm, mixedDoor,
                lineStations, lineDoor, heavyDoor);
        }

        OverworldPressurePlate[] spreadPlates;
        OverworldPuzzleDoor spreadDoor, lineDoor, heavyDoor, mixedDoor;
        OverworldElementalStation[] relayStations, mixedSpreadStations, lineStations;
        OverworldPuzzleDoor[] relayGates;
        OverworldPressurePlate carryPlate;
        OverworldHeavyObject heavyCube;
        OverworldMagicTarget mixedSteam, mixedStorm;

        void BuildCourse()
        {
            RenderSettings.ambientLight = new Color(.53f, .57f, .63f);
            RenderSettings.fog = false;

            // START and LINE: individual leaders activate matching elemental stations.
            Platform("START | LINE", new Vector3(0, -.5f, 2), new Vector3(18, 1, 24), new Color(.24f,.29f,.33f));
            lineStations = new[] {
                Station("Brim • Fire", Element.Fire, new Vector3(-5, .35f, 8)),
                Station("Brooke • Water", Element.Water, new Vector3(0, .35f, 8)),
                Station("Blitz • Electric", Element.Electric, new Vector3(5, .35f, 8)) };
            lineDoor = Door("Line Gate", new Vector3(0, 1.4f, 12));
            for (int i=0;i<lineStations.Length;i++) lineStations[i].Available = i == 0;
            Block("Fire route marker", ElementColor(Element.Fire), new Vector3(-5,.6f,5), new Vector3(1,.8f,1));
            Block("Water route marker", ElementColor(Element.Water), new Vector3(0,.6f,5), new Vector3(1,.8f,1));
            Block("Electric route marker", ElementColor(Element.Electric), new Vector3(5,.6f,5), new Vector3(1,.8f,1));

            // SPREAD: three simultaneous plates, then an element-gated relay.
            Platform("SPREAD | Plates and Relay", new Vector3(0,-.5f,34), new Vector3(18,1,40), new Color(.29f,.32f,.34f));
            spreadPlates = new[] { Plate("Plate A",0,new Vector3(-5,.08f,22)), Plate("Plate B",1,new Vector3(0,.08f,22)), Plate("Plate C",2,new Vector3(5,.08f,22)) };
            spreadDoor = Door("Three Plate Door", new Vector3(0,1.4f,30));
            relayStations = new[] {
                Station("Relay 1 • Fire opens Brooke's path",Element.Fire,new Vector3(-5,.35f,36),true),
                Station("Relay 2 • Water opens Blitz's path",Element.Water,new Vector3(0,.35f,41),false),
                Station("Relay 3 • Electric opens exit",Element.Electric,new Vector3(5,.35f,46),false) };
            relayGates = new[] { Door("Brooke Path Gate",new Vector3(-2,1.4f,39)), Door("Blitz Path Gate",new Vector3(2,1.4f,44)), Door("Spread Exit",new Vector3(0,1.4f,51)) };

            // TOTEM: steam pump, remote storm target, plasma barrier.
            Platform("TOTEM | Synchronized Magic",new Vector3(0,-.5f,59),new Vector3(18,1,24),new Color(.32f,.34f,.36f));
            var steamPump=Magic("Steam Pump",OverworldMagicKind.Steam,new Vector3(0,.8f,58),true);
            steamPump.LinkedDoor=Door("Steam Pump Gate",new Vector3(0,1.4f,61));
            var stormGenerator=Magic("Remote Storm Generator",OverworldMagicKind.Storm,new Vector3(6,.8f,65));
            stormGenerator.LinkedDoor=Door("Storm Generator Gate",new Vector3(0,1.4f,68));
            var plasma = Magic("Reinforced Barrier • Plasma",OverworldMagicKind.Plasma,new Vector3(0,1.4f,70));
            plasma.DestroyOnActivate = true;
            Block("Pump pipe",new Color(.55f,.64f,.68f),new Vector3(0,.3f,56),new Vector3(.35f,.6f,4));

            // HUDDLE: heavy carry, air dash gap, then high launch to distant lower glide platform.
            Platform("HUDDLE | Heavy Carry",new Vector3(0,-.5f,78),new Vector3(18,1,16),new Color(.28f,.32f,.35f));
            heavyCube = Heavy("Heavy Cube",new Vector3(-4,.6f,77));
            carryPlate = Plate("Heavy Pressure Plate",3,new Vector3(4,.08f,77),true);
            heavyDoor = Door("Heavy Door",new Vector3(0,1.4f,84));
            Platform("Dash Approach",new Vector3(0,-.5f,90),new Vector3(12,1,6),new Color(.31f,.34f,.36f));
            Platform("Dash Landing",new Vector3(0,-.5f,104),new Vector3(12,1,8),new Color(.33f,.36f,.38f));
            Platform("Glide Launch (high)",new Vector3(0,7.5f,123.9f),new Vector3(12,1,8),new Color(.37f,.39f,.4f));
            Platform("Glide Landing (low and far)",new Vector3(0,-.5f,143),new Vector3(14,1,14),new Color(.32f,.36f,.38f));
            // Each tread rises .72m; the high platform starts beyond the stair edges so it cannot block the climb from above.
            for (int i=0;i<10;i++) Platform("Glide Stair "+(i+1),new Vector3(0,.36f+i*.72f,109+i*1.05f),new Vector3(5,.72f,2.5f),new Color(.4f,.43f,.44f));

            // MIXED: Spread switches, Steam, Storm, then Huddle dash to finish.
            Platform("MIXED | Mechanisms",new Vector3(0,-.5f,152),new Vector3(20,1,20),new Color(.29f,.33f,.36f));
            mixedSpreadStations = new[] {
                Station("Mixed Switch 1 • Brim",Element.Fire,new Vector3(-6,.35f,151)),
                Station("Mixed Switch 2 • Brooke",Element.Water,new Vector3(0,.35f,151)),
                Station("Mixed Switch 3 • Blitz",Element.Electric,new Vector3(6,.35f,151)) };
            mixedSteam = Magic("Mixed Steam Machine",OverworldMagicKind.Steam,new Vector3(0,.8f,158),false);
            mixedStorm = Magic("Mixed Storm Generator",OverworldMagicKind.Storm,new Vector3(6,.8f,163),false);
            mixedDoor = Door("Mixed Exit",new Vector3(0,1.4f,167));
            Platform("Final Dash Approach",new Vector3(0,-.5f,174),new Vector3(12,1,6),new Color(.31f,.35f,.38f));
            Platform("FINISH",new Vector3(0,-.5f,188),new Vector3(16,1,10),new Color(.2f,.48f,.34f));

            // Side rails on the compact lane keep course navigation readable.
            Block("West course boundary",new Color(.18f,.21f,.24f),new Vector3(-10,2,90),new Vector3(1,6,120));
            Block("East course boundary",new Color(.18f,.21f,.24f),new Vector3(10,2,90),new Vector3(1,6,120));
        }

        void Update()
        {
            if (!party || !party.Formations) return;
            if (party.GroupAnchor && party.GroupAnchor.position.y < -20f)
            {
                party.GroupMotor.SnapTo(new Vector3(0,0,0),Quaternion.identity);
                party.Say("Returned to the start. Try a different formation.");
            }
        }

        void LateUpdate()
        {
            if (!sceneCamera || !party || party.Controlled == null) return;
        }

        void BuildCameraAndLight()
        {
            sceneCamera = Camera.main;
            if (!sceneCamera)
            {
                var cameraObject = new GameObject("Main Camera"); cameraObject.tag="MainCamera";
                sceneCamera=cameraObject.AddComponent<Camera>(); cameraObject.AddComponent<AudioListener>();
            }
            sceneCamera.fieldOfView=68; sceneCamera.nearClipPlane=.08f;
            if (!FindFirstObjectByType<Light>())
            {
                var lightObject=new GameObject("Overworld Sun"); var light=lightObject.AddComponent<Light>();
                light.type=LightType.Directional; light.intensity=1.25f; lightObject.transform.rotation=Quaternion.Euler(48,-28,0);
            }
        }

        OverworldElementalStation Station(string label,Element element,Vector3 pos,bool available=true)
        {
            pos.y=1.2f;
            var obj=Primitive(label,PrimitiveType.Cube,pos,new Vector3(1.6f,2.4f,1f),ElementColor(element));
            var station=obj.AddComponent<OverworldElementalStation>(); station.Initialize(element,label,available); return station;
        }
        OverworldMagicTarget Magic(string label,OverworldMagicKind kind,Vector3 pos,bool available=true)
        {
            bool barrier=kind==OverworldMagicKind.Plasma;
            var obj=Primitive(label,barrier?PrimitiveType.Cube:PrimitiveType.Cylinder,pos,barrier?new Vector3(5,3,.6f):new Vector3(1.2f,.45f,1.2f),kind==OverworldMagicKind.Steam?new Color(.77f,.9f,.94f):kind==OverworldMagicKind.Storm?new Color(.42f,.68f,1f):new Color(1f,.46f,.13f));
            var target=obj.AddComponent<OverworldMagicTarget>(); target.Initialize(kind,label,available); return target;
        }
        OverworldPressurePlate Plate(string label,int id,Vector3 pos,bool heavy=false)
        { var obj=Primitive(label,PrimitiveType.Cube,pos,new Vector3(2,.16f,2),new Color(.35f,.42f,.5f)); var p=obj.AddComponent<OverworldPressurePlate>();p.Initialize(id,heavy);return p; }
        OverworldHeavyObject Heavy(string label,Vector3 pos)
        { var obj=Primitive(label,PrimitiveType.Cube,pos,new Vector3(1.2f,1.2f,1.2f),new Color(.55f,.57f,.6f)); var body=obj.AddComponent<Rigidbody>();body.mass=8;var heavy=obj.AddComponent<OverworldHeavyObject>();heavy.SetCarried(false);return heavy; }
        OverworldPuzzleDoor Door(string label,Vector3 pos)
        { var obj=Primitive(label,PrimitiveType.Cube,pos,new Vector3(5,3,.6f),new Color(.55f,.2f,.18f));obj.AddComponent<OverworldPuzzleDoor>();return obj.GetComponent<OverworldPuzzleDoor>(); }
        void Platform(string label,Vector3 pos,Vector3 size,Color color)=>Primitive(label,PrimitiveType.Cube,pos,size,color);
        void Block(string label,Color color,Vector3 pos,Vector3 size)=>Primitive(label,PrimitiveType.Cube,pos,size,color);
        GameObject Primitive(string label,PrimitiveType type,Vector3 pos,Vector3 size,Color color)
        { var obj=GameObject.CreatePrimitive(type);obj.name=label;obj.transform.position=pos;obj.transform.localScale=size;obj.GetComponent<Renderer>().material=RuntimeMaterials.Lit(color);return obj; }
        static Color ElementColor(Element e)=>e==Element.Fire?new Color(1,.2f,.08f):e==Element.Water?new Color(.08f,.5f,1):new Color(1,.82f,.05f);

        void OnGUI()
        {
            if (titleStyle==null){titleStyle=new GUIStyle(GUI.skin.label){fontSize=20,fontStyle=FontStyle.Bold};titleStyle.normal.textColor=Color.white;bodyStyle=new GUIStyle(GUI.skin.label){fontSize=14,wordWrap=true};bodyStyle.normal.textColor=new Color(.9f,.94f,1);hintStyle=new GUIStyle(bodyStyle){fontSize=12};hintStyle.normal.textColor=new Color(.72f,.82f,.91f);}
            GUI.color=new Color(.035f,.055f,.075f,.9f);GUI.Box(new Rect(14,14,460,154),GUIContent.none);GUI.color=Color.white;
            if (party && party.Formations)
            {
                string controlled=party.Controlled?party.Controlled.name:"Party initializing";
                GUI.Label(new Rect(28,22,430,28),$"FORMATION: {party.Formations.Current}   |   CONTROL: {controlled}",titleStyle);
                string detail=party.Formations.Current==FormationKind.Totem?$"Top + Middle: {party.Formations.TotemPair}   |   Bottom: {party.NameOf(party.Formations.Bottom)}":party.Formations.Current==FormationKind.Line?$"Leader: {(party.Leader?party.Leader.name:"Party initializing")}  |  {party.Message}":party.Message;
                GUI.Label(new Rect(28,54,430,45),detail,bodyStyle);
            }
            GUI.Label(new Rect(28,102,500,58),"WASD / stick move • Space / A jump • Alt sprint • Q/E Line leader or Spread control\n1–4 formations • Q / LT cycle Totem • F / X element or Huddle pickup • G / Y interact • Shift / B Huddle dash • C / RT glide",hintStyle);
            GUI.Label(new Rect(18,Screen.height-32,1000,24),party?party.Message:prompt,hintStyle);
        }
    }
}
