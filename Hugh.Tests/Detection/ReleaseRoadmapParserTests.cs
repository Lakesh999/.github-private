using Hugh.Detection;

namespace Hugh.Tests.Detection;

/// <summary>
/// Unit tests for ReleaseRoadmapParser.
/// Tests parsing of various roadmap document formats and edge cases.
/// </summary>
public class ReleaseRoadmapParserTests
{
    private readonly ReleaseRoadmapParser _parser = new();

    #region ParseReleases — Standard Format

    [Fact]
    public void ParseReleases_StandardFormat_ParsesAllReleases()
    {
        var roadmap = """
            # Release Roadmap: Task Management Platform

            ## Solution Summary
            A platform for managing tasks.

            ## Releases

            ### Release 1: MVP — Core Task Management
            **Goal:** Basic task CRUD operations
            **Scope:**
            - Task creation and editing
            - Task list view
            - User authentication
            **Dependencies:** None

            ### Release 2: Collaboration Features
            **Goal:** Team collaboration on tasks
            **Scope:**
            - Shared task boards
            - Comments on tasks
            - Team invitations
            **Dependencies:** Release 1 (requires user auth)

            ### Release 3: Real-Time Updates — Vision Realized
            **Goal:** Live updates across all clients
            **Scope:**
            - WebSocket integration
            - Real-time notifications
            **Dependencies:** Release 2 (requires collaboration features)
            """;

        var releases = _parser.ParseReleases(roadmap);

        Assert.Equal(3, releases.Count);
    }

    [Fact]
    public void ParseReleases_StandardFormat_ParsesReleaseNumbers()
    {
        var roadmap = """
            ### Release 1: MVP
            **Goal:** Basic features
            **Scope:**
            - Feature A

            ### Release 2: Extended
            **Goal:** More features
            **Scope:**
            - Feature B
            """;

        var releases = _parser.ParseReleases(roadmap);

        Assert.Equal(1, releases[0].Number);
        Assert.Equal(2, releases[1].Number);
    }

    [Fact]
    public void ParseReleases_StandardFormat_ParsesNames()
    {
        var roadmap = """
            ### Release 1: MVP — Core Task Management
            **Goal:** Basic task management

            ### Release 2: Collaboration Features
            **Goal:** Team features
            """;

        var releases = _parser.ParseReleases(roadmap);

        Assert.Equal("MVP — Core Task Management", releases[0].Name);
        Assert.Equal("Collaboration Features", releases[1].Name);
    }

    [Fact]
    public void ParseReleases_StandardFormat_ParsesGoals()
    {
        var roadmap = """
            ### Release 1: MVP
            **Goal:** Enable basic task creation and management for individual users
            **Scope:**
            - Task CRUD
            """;

        var releases = _parser.ParseReleases(roadmap);

        Assert.Equal("Enable basic task creation and management for individual users", releases[0].Goal);
    }

    [Fact]
    public void ParseReleases_StandardFormat_ParsesScopeItems()
    {
        var roadmap = """
            ### Release 1: MVP
            **Goal:** Basic features
            **Scope:**
            - Task creation and editing
            - Task list view with filtering
            - User authentication via OAuth
            **Dependencies:** None
            """;

        var releases = _parser.ParseReleases(roadmap);

        Assert.Equal(3, releases[0].Scope.Count);
        Assert.Equal("Task creation and editing", releases[0].Scope[0]);
        Assert.Equal("Task list view with filtering", releases[0].Scope[1]);
        Assert.Equal("User authentication via OAuth", releases[0].Scope[2]);
    }

    [Fact]
    public void ParseReleases_StandardFormat_ParsesDependencies()
    {
        var roadmap = """
            ### Release 1: MVP
            **Goal:** Basic features
            **Scope:**
            - Feature A
            **Dependencies:** None

            ### Release 2: Extended
            **Goal:** More features
            **Scope:**
            - Feature B
            **Dependencies:** Release 1 (requires auth)

            ### Release 3: Final
            **Goal:** Polish
            **Scope:**
            - Feature C
            **Dependencies:** Release 1, Release 2
            """;

        var releases = _parser.ParseReleases(roadmap);

        Assert.Empty(releases[0].Dependencies);
        Assert.Single(releases[1].Dependencies);
        Assert.Equal(1, releases[1].Dependencies[0]);
        Assert.Equal(2, releases[2].Dependencies.Count);
        Assert.Contains(1, releases[2].Dependencies);
        Assert.Contains(2, releases[2].Dependencies);
    }

    #endregion

    #region ParseReleases — Edge Cases

    [Fact]
    public void ParseReleases_EmptyContent_ReturnsEmptyList()
    {
        var releases = _parser.ParseReleases("");

        Assert.Empty(releases);
    }

    [Fact]
    public void ParseReleases_NullContent_ReturnsEmptyList()
    {
        var releases = _parser.ParseReleases(null!);

        Assert.Empty(releases);
    }

    [Fact]
    public void ParseReleases_WhitespaceOnly_ReturnsEmptyList()
    {
        var releases = _parser.ParseReleases("   \n\n  ");

        Assert.Empty(releases);
    }

    [Fact]
    public void ParseReleases_NoReleaseHeaders_ReturnsEmptyList()
    {
        var roadmap = """
            # Some Document

            This is just regular content with no release headers.

            ## Section
            Some text.
            """;

        var releases = _parser.ParseReleases(roadmap);

        Assert.Empty(releases);
    }

    [Fact]
    public void ParseReleases_SingleRelease_ParsesCorrectly()
    {
        var roadmap = """
            ### Release 1: MVP — Complete Solution
            **Goal:** Everything in one release
            **Scope:**
            - Feature A
            - Feature B
            """;

        var releases = _parser.ParseReleases(roadmap);

        Assert.Single(releases);
        Assert.Equal(1, releases[0].Number);
        Assert.Equal("MVP — Complete Solution", releases[0].Name);
    }

    [Fact]
    public void ParseReleases_MissingGoal_DefaultsToEmpty()
    {
        var roadmap = """
            ### Release 1: MVP
            **Scope:**
            - Feature A
            """;

        var releases = _parser.ParseReleases(roadmap);

        Assert.Single(releases);
        Assert.Equal("", releases[0].Goal);
    }

    [Fact]
    public void ParseReleases_MissingScope_ReturnsEmptyScope()
    {
        var roadmap = """
            ### Release 1: MVP
            **Goal:** Basic features
            **Dependencies:** None
            """;

        var releases = _parser.ParseReleases(roadmap);

        Assert.Single(releases);
        Assert.Empty(releases[0].Scope);
    }

    [Fact]
    public void ParseReleases_MissingDependencies_ReturnsEmptyDependencies()
    {
        var roadmap = """
            ### Release 1: MVP
            **Goal:** Basic features
            **Scope:**
            - Feature A
            """;

        var releases = _parser.ParseReleases(roadmap);

        Assert.Single(releases);
        Assert.Empty(releases[0].Dependencies);
    }

    [Fact]
    public void ParseReleases_DependencyNone_ReturnsEmptyDependencies()
    {
        var roadmap = """
            ### Release 1: MVP
            **Goal:** Basic features
            **Scope:**
            - Feature A
            **Dependencies:** None
            """;

        var releases = _parser.ParseReleases(roadmap);

        Assert.Empty(releases[0].Dependencies);
    }

    [Fact]
    public void ParseReleases_DependencyNA_ReturnsEmptyDependencies()
    {
        var roadmap = """
            ### Release 1: MVP
            **Goal:** Basic features
            **Scope:**
            - Feature A
            **Dependencies:** N/A
            """;

        var releases = _parser.ParseReleases(roadmap);

        Assert.Empty(releases[0].Dependencies);
    }

    [Fact]
    public void ParseReleases_OrdersReleasesByNumber()
    {
        var roadmap = """
            ### Release 3: Final
            **Goal:** Polish

            ### Release 1: MVP
            **Goal:** Basic

            ### Release 2: Extended
            **Goal:** More
            """;

        var releases = _parser.ParseReleases(roadmap);

        Assert.Equal(3, releases.Count);
        Assert.Equal(1, releases[0].Number);
        Assert.Equal(2, releases[1].Number);
        Assert.Equal(3, releases[2].Number);
    }

    [Fact]
    public void ParseReleases_DuplicateDependencies_DeduplicatesAndSorts()
    {
        var roadmap = """
            ### Release 1: MVP
            **Goal:** Basic

            ### Release 3: Final
            **Goal:** Polish
            **Dependencies:** Release 1, Release 2, Release 1, Release 2
            """;

        var releases = _parser.ParseReleases(roadmap);

        var finalRelease = releases.First(r => r.Number == 3);
        Assert.Equal(2, finalRelease.Dependencies.Count);
        Assert.Equal(1, finalRelease.Dependencies[0]);
        Assert.Equal(2, finalRelease.Dependencies[1]);
    }

    #endregion

    #region ParseReleases — Alternate Formats

    [Fact]
    public void ParseReleases_AlternateScope_ParsesWithDoubleAsterisks()
    {
        var roadmap = """
            ### Release 1: MVP
            **Goal:** Basic features
            **Scope**
            - Feature A
            - Feature B
            **Acceptance Criteria:**
            - Criterion 1
            """;

        var releases = _parser.ParseReleases(roadmap);

        Assert.Equal(2, releases[0].Scope.Count);
        Assert.Equal("Feature A", releases[0].Scope[0]);
        Assert.Equal("Feature B", releases[0].Scope[1]);
    }

    [Fact]
    public void ParseReleases_DependenciesWithParenthetical_ParsesCorrectly()
    {
        var roadmap = """
            ### Release 1: MVP
            **Goal:** Basic

            ### Release 2: Extended
            **Goal:** More
            **Dependencies:** Release 1 (requires auth infrastructure)
            """;

        var releases = _parser.ParseReleases(roadmap);

        Assert.Single(releases[1].Dependencies);
        Assert.Equal(1, releases[1].Dependencies[0]);
    }

    #endregion

    #region GetReleaseCount Tests

    [Fact]
    public void GetReleaseCount_ReturnsCorrectCount()
    {
        var roadmap = """
            ### Release 1: MVP
            **Goal:** Basic

            ### Release 2: Extended
            **Goal:** More

            ### Release 3: Final
            **Goal:** Polish
            """;

        var count = _parser.GetReleaseCount(roadmap);

        Assert.Equal(3, count);
    }

    [Fact]
    public void GetReleaseCount_EmptyContent_ReturnsZero()
    {
        Assert.Equal(0, _parser.GetReleaseCount(""));
    }

    [Fact]
    public void GetReleaseCount_NoReleases_ReturnsZero()
    {
        Assert.Equal(0, _parser.GetReleaseCount("Just some text"));
    }

    #endregion

    #region Realistic Decomposition Output

    [Fact]
    public void ParseReleases_RealisticDecompositionOutput_ParsesCorrectly()
    {
        var roadmap = """
            # Release Roadmap: Task Management Platform

            ## Solution Summary
            A comprehensive task management platform that enables individuals and teams to create,
            organize, and track tasks with real-time collaboration features.

            ## Release Overview

            | Release | Name | Focus | Depends On |
            |---------|------|-------|------------|
            | 1 | MVP — Core Task Management | Basic task CRUD and auth | None |
            | 2 | Collaboration Features | Team boards and sharing | Release 1 |
            | 3 | Real-Time & Polish — Vision Realized | Live updates and UX polish | Release 2 |

            ## Releases

            ### Release 1: MVP — Core Task Management
            **Goal:** Enable individual users to create and manage tasks with basic authentication
            **Scope:**
            - User registration and login via OAuth 2.0
            - Task creation with title, description, due date, and priority
            - Task list view with sort and filter capabilities
            - Task status management (todo, in-progress, done)
            - Basic responsive UI with mobile support
            **Dependencies:** None
            **Acceptance Criteria:**
            - Users can register and log in
            - Users can create, edit, and delete tasks
            - Tasks can be filtered by status and priority

            ### Release 2: Collaboration Features
            **Goal:** Enable teams to work together on shared task boards
            **Scope:**
            - Team creation and member management
            - Shared task boards with role-based access
            - Comments and discussions on tasks
            - Email notifications for task assignments
            - Activity feed showing recent changes
            **Dependencies:** Release 1 (requires user authentication and task management)
            **Acceptance Criteria:**
            - Users can create teams and invite members
            - Team boards show shared tasks
            - Comments appear on task detail view

            ### Release 3: Real-Time & Polish — Vision Realized
            **Goal:** Deliver a polished, real-time collaborative experience
            **Scope:**
            - WebSocket-based real-time updates
            - Push notifications for mobile
            - Dark mode and theme customization
            - Performance optimization and caching
            - Comprehensive error handling and offline support
            **Dependencies:** Release 2 (requires collaboration infrastructure)
            **Acceptance Criteria:**
            - Changes propagate to all connected clients instantly
            - App works in offline mode with sync on reconnect
            - Full theme support including dark mode
            """;

        var releases = _parser.ParseReleases(roadmap);

        // Verify all 3 releases parsed
        Assert.Equal(3, releases.Count);

        // Release 1
        Assert.Equal(1, releases[0].Number);
        Assert.Equal("MVP — Core Task Management", releases[0].Name);
        Assert.Contains("individual users", releases[0].Goal);
        Assert.Equal(5, releases[0].Scope.Count);
        Assert.Contains("User registration and login via OAuth 2.0", releases[0].Scope);
        Assert.Empty(releases[0].Dependencies);

        // Release 2
        Assert.Equal(2, releases[1].Number);
        Assert.Equal("Collaboration Features", releases[1].Name);
        Assert.Equal(5, releases[1].Scope.Count);
        Assert.Single(releases[1].Dependencies);
        Assert.Equal(1, releases[1].Dependencies[0]);

        // Release 3
        Assert.Equal(3, releases[2].Number);
        Assert.Equal("Real-Time & Polish — Vision Realized", releases[2].Name);
        Assert.Equal(5, releases[2].Scope.Count);
        Assert.Single(releases[2].Dependencies);
        Assert.Equal(2, releases[2].Dependencies[0]);
    }

    #endregion
}
