using UnityEngine;

namespace Gaskellgames
{
    // <summary>
    // Code created by Gaskellgames
    // </summary>
    
    [System.Serializable]
    public class Password
    {
        [SerializeField] private string password;
        
        public string value
        {
            get { return password; }
            set { password = value; }
        }

        public Password(string newPassword)
        {
            this.value = newPassword;
        }

    } // class end
}