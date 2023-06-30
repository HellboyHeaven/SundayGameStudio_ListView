using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public static class ExtensionTexture
{
    public static async Task<Texture2D> GetUrlImage (string url )
    {
        using( UnityWebRequest www = UnityWebRequestTexture.GetTexture(url) )
        {
            // begin request:
            var asyncOp = www.SendWebRequest();

            // await until it's done: 
            while( asyncOp.isDone==false )
                await Task.Delay( 1000 );//30 hertz
        
            // read results:
            if( www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError )
                // if( www.result!=UnityWebRequest.Result.Success )// for Unity >= 2020.1
            {
                // log error:
#if DEBUG
            Debug.Log( $"{www.error}, URL:{www.url}" );
#endif
            
                // nothing to return on error:
                return null;
            }

            // return valid results:
            return DownloadHandlerTexture.GetContent(www);
        }
    }
}
