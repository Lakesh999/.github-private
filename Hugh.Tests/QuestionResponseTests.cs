using Hugh.Models;

namespace Hugh.Tests;

/// <summary>
/// Unit tests for QuestionResponse factory methods and properties.
/// </summary>
public class QuestionResponseTests
{
    [Fact]
    public void PauseWithQuestion_SetsShouldPause_True()
    {
        var response = QuestionResponse.PauseWithQuestion("Test question?");

        Assert.True(response.ShouldPause);
    }

    [Fact]
    public void PauseWithQuestion_SetsQuestionText()
    {
        var response = QuestionResponse.PauseWithQuestion("Which database should we use?");

        Assert.Equal("Which database should we use?", response.QuestionText);
    }

    [Fact]
    public void PauseWithQuestion_ResponseIsNull()
    {
        var response = QuestionResponse.PauseWithQuestion("Test question?");

        Assert.Null(response.Response);
    }

    [Fact]
    public void Pause_QuestionTextIsNull()
    {
        var response = QuestionResponse.Pause();

        Assert.Null(response.QuestionText);
        Assert.True(response.ShouldPause);
    }
}
