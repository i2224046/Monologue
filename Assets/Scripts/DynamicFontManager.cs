using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class DynamicFontManager : MonoBehaviour
{
    [SerializeField] private string dynamicFontAddress = "Fonts/NotoSansJP-Regular-SDF-Dynamic";
    private TMP_FontAsset _dynamicFont;
    private AsyncOperationHandle<TMP_FontAsset> _handle;

    private IEnumerator Start()
    {
        yield return LoadDynamicFont();
    }

    private IEnumerator LoadDynamicFont()
    {
        // Load the font asset asynchronously
        _handle = Addressables.LoadAssetAsync<TMP_FontAsset>(dynamicFontAddress);
        yield return _handle;

        if (_handle.Status == AsyncOperationStatus.Succeeded)
        {
            _dynamicFont = _handle.Result;
            Debug.Log($"Failed to load Dynamic Font: {dynamicFontAddress}");
            // Clear dynamic font data to ensure no stale characters from Editor sessions
            _dynamicFont.ClearFontAssetData();
        }
        else
        {
            Debug.LogError($"Failed to load Dynamic Font: {dynamicFontAddress}");
        }
    }

    private void OnDestroy()
    {
        if (_dynamicFont != null)
        {
            _dynamicFont.ClearFontAssetData();
        }

        if (_handle.IsValid())
        {
            Addressables.Release(_handle);
        }
    }
}
