using System.Collections;
using System.Text;
using UnityEngine;
using TMPro;

public class GeminiTypewriter : MonoBehaviour
{
    public float delay = 0.05f;
    public string fullText;
    private Coroutine typingCoroutine;

    public void StartTyping(string text)
    {
        fullText = AddLineBreaks(text, 45);

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine = StartCoroutine(TypeText());
    }

    private IEnumerator TypeText()
    {
        TMP_Text tmpText = this.GetComponent<TMP_Text>();
        tmpText.text = ""; // Clear existing text

        foreach (char letter in fullText)
        {
            tmpText.text += letter;
            yield return new WaitForSeconds(delay);
        }
    }

    private string AddLineBreaks(string text, int maxCharactersPerLine)
    {
        string[] segments = text.Split('\n');
        StringBuilder result = new StringBuilder();
        foreach (string segment in segments)
        {
            result.Append(WrapSegment(segment, maxCharactersPerLine));
            result.Append("\n");
        }
        return result.ToString().TrimEnd('\n');
    }

    private string WrapSegment(string segment, int maxCharactersPerLine)
    {
        if (string.IsNullOrEmpty(segment))
            return segment;

        StringBuilder wrapped = new StringBuilder();
        int currentLineLength = 0;
        // Split the segment by spaces
        string[] words = segment.Split(' ');
        foreach (string word in words)
        {
            if (currentLineLength == 0)
            {
                wrapped.Append(word);
                currentLineLength = word.Length;
            }
            else if (currentLineLength + 1 + word.Length > maxCharactersPerLine)
            {
                wrapped.Append("\n" + word);
                currentLineLength = word.Length;
            }
            else
            {
                wrapped.Append(" " + word);
                currentLineLength += 1 + word.Length;
            }
        }
        return wrapped.ToString();
    }
}
