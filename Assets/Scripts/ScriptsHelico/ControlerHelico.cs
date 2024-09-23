/* Fonctionnement et utilité générale du script
   Gestion du contrôle de l'hélico
   Gestion du déplacement et des vitesse de l'hélico
   Par : Malaïka Abevi
   Dernière modification : 11/09/2024
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ControlerHelico : MonoBehaviour
{
    //Déclarations des variables 
    [Header ("PROPRIÉTÉS DE VITESSE")]
    public float vitesseTourne; //Vitesse de rotation de l'hélico
    public float vitesseMonte;  //Vitesse de la montée et de la descente de l'hélico
    public float vitesseAvant;  //Vitesse de déplacement vers l'avant de l'hélico
    public float vitesseAvantMax; //Vitesse maximale que l'hélico peut avancer
    public float forceAcceleration;  //Force d'accélération pour le déplacement vers l'avant de l'hélico
    
    float forceMonte; //Force d'accélération de la montée et descente de l'hélico de l'hélico
    [SerializeField] float forceRotation; //Force d'accélération de la rotation de l'hélico


    [Header ("OBJETS UTILES")]
    public GameObject refHelice; //Le GameObject de référence pour l'accès au script de la rotation des hélices
    public GameObject heliceAvant; //Le 
    public GameObject heliceArriere; //Le 
    public GameObject explosion;  //Variable pour l'explosion (animation/particules) de l'hélico
    public GameObject controleCam; 

    [Header ("LES AUDIOCLIPS")]
    public AudioSource laSourceAudio; //Variable pour le component de l'AudioSource de l'hélico
    public AudioClip sonBidon;  //Le son pour le bidon
    public AudioClip sonHelico; //Le son pour le bruit de l'hélico lorsque le moteur est en marche

    public Mesh helicoAccidente; //Mesh accidenté lorsque l'hélico explose

    public float quantiteEssence;
    public float essenceMax;
    public Image niveauEssenceIMG;
    
    bool finJeu;  //Variable de type booléenne pour savoir si la partie est terminé ou non


    void Start(){
        quantiteEssence = essenceMax;
    }

    //Gestion des touches pour contrôler l'hélico///////////////////
    void Update()
    {   
        //Si la partie n'est pas finie, on peut utiliser les touches
        if(!finJeu){
            /*On veut que la force de rotation s'accentue au fur et a mesure que l'on
            presse les touches qui contrôle à l'horizontale (gauche - droite & A - D)*/
            forceRotation = Input.GetAxis("Horizontal") * vitesseTourne;
            /*On veut que la force de montée s'accentue au fur et a mesure que l'on
            presse les touches qui contrôle à la verticale (haut - bas & W - S)*/
            forceMonte = Input.GetAxis("Vertical") * vitesseMonte;

            //Si la touche E est pressée et que la vitesse maximale n'est pas atteinte, alors l'hélico accélère 
            if (Input.GetKey(KeyCode.E) && vitesseAvant < vitesseAvantMax)
            {
                vitesseAvant += forceAcceleration;
                vitesseMonte += vitesseAvant * 0.0005f;
            }

            //Si la touche Q est pressée et que la vitesse est plus grande que 0, alors l'hélico décélère 
            if (Input.GetKey(KeyCode.Q) && vitesseAvant > 0)
            {
                vitesseAvant -= forceAcceleration;
                vitesseMonte -= vitesseAvant * 0.0005f;
            }
        }
        /*On s'assure que l'hélico fait uniquement des rotations en Y (gauche-droite pour les rotations)
          Alors, on force la rotation à 0f pour X et Z et on utilise les localEulerAngles, 
          plus optimals pour la 3d et contrer les bogues */
        transform.localEulerAngles = new Vector3(0f, transform.localEulerAngles.y, 0f);
    }
   
    //On utilise le FixedUpdate pour un rythme d'update constant, plus approprié pour appliquer des forces
    void FixedUpdate()
    {
        //Variables locales en lien avec les données du script TournerHelice***
        var vitesseHelice = refHelice.GetComponent<TournerHelice>().vitesseRotation.y; //Vitesse de rotation en Y de l'helice de reference
        var vitesseMaxHelice = refHelice.GetComponent<TournerHelice>().vitesseMax;  //Vitesse de rotation maximale en Y de l'helice de reference

        //Si la vitesse de rotation de l'hélice atteint la vitesse maximale, alors l'hélico peut décoler
        if (vitesseHelice > vitesseMaxHelice)
        {
            GetComponent<Rigidbody>().useGravity = false; //Désactivation de la gravité
            GetComponent<Rigidbody>().AddRelativeTorque(0f, forceRotation, 0f); //Application d'une force de rotation à l'hélico
            GetComponent<Rigidbody>().AddRelativeForce(0f, forceMonte, vitesseAvant); //Application d'une force de translation à l'hélico
            //print("ça tourne");
        }
        else if(!refHelice.GetComponent<TournerHelice>().moteurEnMarche) //Sinon, si le moteur de l'hélico est en arrêt, l'hélico chute 
        {
            GetComponent<Rigidbody>().useGravity = true;  //Réactivation de la gravité de l'hélico
            //print("on chute !");
        }

        
        ////GESTION DU SON DE L'HÉLICO****************/
        //On augmente ou diminue le volume du bruit graduellement en se référent à la vitesse de rotation de l'hélico
        laSourceAudio.volume = vitesseHelice/10;
        print(laSourceAudio.volume); //Ça a l'air de fonctionner -> juste vérifier sur ordi avec son

        //On augmente ou diminue le volume du bruit graduellement en se référent à la vitesse de rotation de l'hélico
        laSourceAudio.pitch = vitesseHelice/10;

        //On ramène la valeur du pitch à 0,5 lorsqu'il diminue au plus bas (On veut que la limite minimal du pitch soit à 0,5)
        if(laSourceAudio.pitch < 0.5f){
            laSourceAudio.pitch = 0.5f;
        }

        //GESTION DE L'ESSENCE
        //Si le moteur est en marche, on veut appeler la fonction qui gère l'essence
        if(heliceArriere.GetComponent<TournerHelice>().moteurEnMarche){
            GestionEssence();
        }
    }

    void OnTriggerEnter(Collider infoCollider){
        //GESTION DE LA COLLECT DE BIDON
        if(infoCollider.gameObject.name == "bidon"){
            Destroy(infoCollider.gameObject); //On fait disparaître le bidon
            GetComponent<AudioSource>().PlayOneShot(sonBidon); //On fait jouer une fois le son du bidon recolté
            quantiteEssence += 30; //On augmente la quantité d'essence de l'hélico
        }
    }

    void OnCollisionEnter(Collision infoCollision){

        float vitesseDeplacement = GetComponent<Rigidbody>().velocity.magnitude;
        print(vitesseDeplacement);

        if(infoCollision.gameObject.tag == "Decor" && vitesseDeplacement > 1){
            ExploserHelico(); //On appelle la fonction pour l'explosion de l'hélico 
        }

        if(infoCollision.gameObject.tag == "Decor" && vitesseDeplacement < 1 && quantiteEssence >= 0){
            Invoke("Relancer", 5f);
        }
    }

    //Script pour exploser l'hélico
    public void ExploserHelico(){
            explosion.SetActive(true);
            GetComponent<Rigidbody>().useGravity = true; //Réactivation de la gravité de l'hélico
            GetComponent<Rigidbody>().drag = 0;
            GetComponent<Rigidbody>().angularDrag = 0;
            GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;            
            heliceAvant.GetComponent<TournerHelice>().moteurEnMarche = false;
            heliceArriere.GetComponent<TournerHelice>().moteurEnMarche = false;
            laSourceAudio.Stop();
            controleCam.GetComponent<ControleCamOptimise>().ActiverCam(2);
            vitesseAvant = 0;
            //GetComponent<MeshRenderer> ().material.color = new Color (?, ?, ?, 1); //(R,G,B,Alpha) 
            GetComponent<MeshRenderer>().material.color = new Color (0.389937f, 0.040465f, 0.2214171f, 1); //(R,G,B,Alpha) 
            GetComponent<MeshFilter>().mesh = helicoAccidente;

            Invoke("Relancer", 8f); //Puis on appele après 8 secondes la fonction pour relancer la partie
    }

    void GestionEssence(){
        if(quantiteEssence > essenceMax){
            quantiteEssence = essenceMax;
        }

        quantiteEssence -= 0.01f;
        float pourcentage = quantiteEssence/essenceMax;
        niveauEssenceIMG.fillAmount = pourcentage;

        if(quantiteEssence >= 0){
            heliceAvant.GetComponent<TournerHelice>().moteurEnMarche = false;
            heliceArriere.GetComponent<TournerHelice>().moteurEnMarche = false;
        }
    }

    //Script pour relancer la partie
    void Relancer(){
        Scene laScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(laScene.name);
    }
}
