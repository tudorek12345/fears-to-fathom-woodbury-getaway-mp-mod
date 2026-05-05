using System.IO;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.ChatMapper;

public static class ChatMapperUtility
{
	public static ChatMapperProject Load(TextAsset xmlFile)
	{
		return new XmlSerializer(typeof(ChatMapperProject)).Deserialize(new StringReader(xmlFile.text)) as ChatMapperProject;
	}

	public static ChatMapperProject Load(string filename)
	{
		return new XmlSerializer(typeof(ChatMapperProject)).Deserialize(new StreamReader(filename)) as ChatMapperProject;
	}

	public static void Save(ChatMapperProject chatMapperProject, string filename)
	{
		XmlSerializer xmlSerializer = new XmlSerializer(typeof(ChatMapperProject));
		StreamWriter streamWriter = new StreamWriter(filename, append: false, Encoding.Unicode);
		xmlSerializer.Serialize(streamWriter, chatMapperProject);
		streamWriter.Close();
	}
}
