using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace PixelCrushers;

public class EncryptionUtility
{
	private const int Iterations = 1000;

	public static string Encrypt(string plainText, string password)
	{
		if (string.IsNullOrEmpty(plainText) || string.IsNullOrEmpty(password))
		{
			return string.Empty;
		}
		DESCryptoServiceProvider dESCryptoServiceProvider = new DESCryptoServiceProvider();
		dESCryptoServiceProvider.GenerateIV();
		byte[] bytes = new Rfc2898DeriveBytes(password, dESCryptoServiceProvider.IV, 1000).GetBytes(8);
		using MemoryStream memoryStream = new MemoryStream();
		using CryptoStream cryptoStream = new CryptoStream(memoryStream, dESCryptoServiceProvider.CreateEncryptor(bytes, dESCryptoServiceProvider.IV), CryptoStreamMode.Write);
		memoryStream.Write(dESCryptoServiceProvider.IV, 0, dESCryptoServiceProvider.IV.Length);
		byte[] bytes2 = Encoding.UTF8.GetBytes(plainText);
		cryptoStream.Write(bytes2, 0, bytes2.Length);
		cryptoStream.FlushFinalBlock();
		return Convert.ToBase64String(memoryStream.ToArray());
	}

	public static bool TryDecrypt(string cipherText, string password, out string plainText)
	{
		if (string.IsNullOrEmpty(cipherText) || string.IsNullOrEmpty(password))
		{
			plainText = string.Empty;
			return false;
		}
		try
		{
			using MemoryStream memoryStream = new MemoryStream(Convert.FromBase64String(cipherText));
			DESCryptoServiceProvider dESCryptoServiceProvider = new DESCryptoServiceProvider();
			byte[] array = new byte[8];
			memoryStream.Read(array, 0, array.Length);
			byte[] bytes = new Rfc2898DeriveBytes(password, array, 1000).GetBytes(8);
			using CryptoStream stream = new CryptoStream(memoryStream, dESCryptoServiceProvider.CreateDecryptor(bytes, array), CryptoStreamMode.Read);
			using StreamReader streamReader = new StreamReader(stream);
			plainText = streamReader.ReadToEnd();
			return true;
		}
		catch (Exception ex)
		{
			Debug.LogError("Dialogue System Menus: Can't decrypt data: + " + ex.Message);
			plainText = string.Empty;
			return false;
		}
	}
}
