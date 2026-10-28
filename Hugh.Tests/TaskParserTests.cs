using Hugh.Detection;

namespace Hugh.Tests;

/// <summary>
/// Unit tests for TaskParser.
/// Tests parsing of task rows from plan tables within specific phases,
/// finding the next incomplete task, and phase isolation.
/// </summary>
public class TaskParserTests
{
    private readonly TaskParser _parser = new();

    #region ParseTasks

    [Fact]
    public void ParseTasks_WithValidPhase_ReturnsAllTasks()
    {
        var content = """
            ## Execution Plan

            ### Phase 3: API Implementation

            **Status:** ⏳ Not Started

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 3.1 | Create API controller | `src/Controllers/ApiController.cs` | Controller handles GET and POST |
            | 3.2 | Add validation middleware | `src/Middleware/ValidationMiddleware.cs` | Validates all incoming requests |
            | 3.3 | Write integration tests | `tests/ApiTests.cs` | All endpoints tested |

            ### Phase 4: UI Implementation

            **Status:** ⏳ Not Started
            """;

        var tasks = _parser.ParseTasks(content, 3);

        Assert.Equal(3, tasks.Count);

        Assert.Equal(3, tasks[0].PhaseNumber);
        Assert.Equal(1, tasks[0].TaskNumber);
        Assert.Equal("3.1", tasks[0].StepId);
        Assert.Equal("Create API controller", tasks[0].Description);
        Assert.False(tasks[0].IsComplete);
        Assert.Contains("ApiController.cs", tasks[0].Files);
        Assert.Contains("GET and POST", tasks[0].AcceptanceCriteria);

        Assert.Equal("3.2", tasks[1].StepId);
        Assert.Equal("Add validation middleware", tasks[1].Description);

        Assert.Equal("3.3", tasks[2].StepId);
        Assert.Equal("Write integration tests", tasks[2].Description);
    }

    [Fact]
    public void ParseTasks_WithCompletedTasks_DetectsCompletionStatus()
    {
        var content = """
            ### Phase 2: Core Logic

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 2.1 | ✅ Create data model | `src/Models/Data.cs` | Model has all properties |
            | 2.2 | Implement service layer | `src/Services/DataService.cs` | Service methods complete |
            | 2.3 | ✅ Add repository | `src/Repos/DataRepo.cs` | CRUD operations |
            """;

        var tasks = _parser.ParseTasks(content, 2);

        Assert.Equal(3, tasks.Count);
        Assert.True(tasks[0].IsComplete);  // 2.1 has ✅
        Assert.False(tasks[1].IsComplete); // 2.2 does not
        Assert.True(tasks[2].IsComplete);  // 2.3 has ✅
    }

    [Fact]
    public void ParseTasks_WithEmptyContent_ReturnsEmpty()
    {
        var tasks = _parser.ParseTasks("", 1);
        Assert.Empty(tasks);
    }

    [Fact]
    public void ParseTasks_WithNullContent_ReturnsEmpty()
    {
        var tasks = _parser.ParseTasks(null!, 1);
        Assert.Empty(tasks);
    }

    [Fact]
    public void ParseTasks_WithNoMatchingPhase_ReturnsEmpty()
    {
        var content = """
            ### Phase 1: Setup

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 1.1 | Setup project | `project.csproj` | Project builds |
            """;

        var tasks = _parser.ParseTasks(content, 99);
        Assert.Empty(tasks);
    }

    [Fact]
    public void ParseTasks_IsolatesTasksToRequestedPhase()
    {
        var content = """
            ### Phase 1: Setup

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 1.1 | Setup project | `project.csproj` | Project builds |
            | 1.2 | Add config | `appsettings.json` | Config loads |

            ### Phase 2: Core Logic

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 2.1 | Create model | `Model.cs` | Model correct |
            | 2.2 | Add service | `Service.cs` | Service works |
            | 2.3 | Write tests | `Tests.cs` | Tests pass |

            ### Phase 3: Deployment

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 3.1 | Deploy script | `deploy.sh` | Script runs |
            """;

        var phase1Tasks = _parser.ParseTasks(content, 1);
        var phase2Tasks = _parser.ParseTasks(content, 2);
        var phase3Tasks = _parser.ParseTasks(content, 3);

        Assert.Equal(2, phase1Tasks.Count);
        Assert.All(phase1Tasks, t => Assert.Equal(1, t.PhaseNumber));

        Assert.Equal(3, phase2Tasks.Count);
        Assert.All(phase2Tasks, t => Assert.Equal(2, t.PhaseNumber));

        Assert.Single(phase3Tasks);
        Assert.Equal(3, phase3Tasks[0].PhaseNumber);
    }

    [Fact]
    public void ParseTasks_WithPhaseHavingNoTaskTable_ReturnsEmpty()
    {
        var content = """
            ### Phase 1: Setup

            **Status:** ⏳ Not Started
            **Size:** Small

            This phase has no task table yet.

            ### Phase 2: Core Logic

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 2.1 | Create model | `Model.cs` | Model correct |
            """;

        var tasks = _parser.ParseTasks(content, 1);
        Assert.Empty(tasks);
    }

    [Fact]
    public void ParseTasks_WithBacktickWrappedFiles_ParsesCorrectly()
    {
        var content = """
            ### Phase 1: Setup

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 1.1 | Create parser | `Hugh/Detection/TaskParser.cs` | Parses correctly |
            """;

        var tasks = _parser.ParseTasks(content, 1);

        Assert.Single(tasks);
        Assert.Contains("TaskParser.cs", tasks[0].Files);
    }

    [Fact]
    public void ParseTasks_WithCompletionEmojiAnywhereInRow_DetectsAsComplete()
    {
        var content = """
            ### Phase 4: Testing

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 4.1 | Write unit tests | `Tests.cs` | ✅ All tests pass |
            """;

        var tasks = _parser.ParseTasks(content, 4);

        Assert.Single(tasks);
        Assert.True(tasks[0].IsComplete);
    }

    [Fact]
    public void ParseTasks_WithDashSeparator_ReturnsTasks()
    {
        var content = """
            ### Phase 3 - API Implementation

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 3.1 | Create API controller | `src/Controllers/ApiController.cs` | Controller handles GET and POST |
            | 3.2 | Add validation | `src/Middleware/Validation.cs` | Validates requests |
            """;

        var tasks = _parser.ParseTasks(content, 3);

        Assert.Equal(2, tasks.Count);
        Assert.Equal("3.1", tasks[0].StepId);
        Assert.Equal("Create API controller", tasks[0].Description);
        Assert.Equal("3.2", tasks[1].StepId);
    }

    [Fact]
    public void ParseTasks_WithEmDashSeparator_ReturnsTasks()
    {
        var content = """
            ### Phase 3 — API Implementation

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 3.1 | Create API controller | `src/Controllers/ApiController.cs` | Controller handles GET and POST |
            | 3.2 | Add validation | `src/Middleware/Validation.cs` | Validates requests |
            """;

        var tasks = _parser.ParseTasks(content, 3);

        Assert.Equal(2, tasks.Count);
        Assert.Equal("3.1", tasks[0].StepId);
        Assert.Equal("Create API controller", tasks[0].Description);
        Assert.Equal("3.2", tasks[1].StepId);
    }

    [Fact]
    public void ParseTasks_WithEnDashSeparator_ReturnsTasks()
    {
        var content = """
            ### Phase 3 – API Implementation

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 3.1 | Create API controller | `src/Controllers/ApiController.cs` | Controller handles GET and POST |
            | 3.2 | Add validation | `src/Middleware/Validation.cs` | Validates requests |
            """;

        var tasks = _parser.ParseTasks(content, 3);

        Assert.Equal(2, tasks.Count);
        Assert.Equal("3.1", tasks[0].StepId);
        Assert.Equal("Create API controller", tasks[0].Description);
        Assert.Equal("3.2", tasks[1].StepId);
    }

    #endregion

    #region FindNextIncompleteTask

    [Fact]
    public void FindNextIncompleteTask_ReturnsFirstIncomplete()
    {
        var content = """
            ### Phase 2: Implementation

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 2.1 | ✅ Create model | `Model.cs` | Model correct |
            | 2.2 | ✅ Add repository | `Repo.cs` | CRUD works |
            | 2.3 | Implement service | `Service.cs` | Service complete |
            | 2.4 | Write tests | `Tests.cs` | Tests pass |
            """;

        var task = _parser.FindNextIncompleteTask(content, 2);

        Assert.NotNull(task);
        Assert.Equal("2.3", task.StepId);
        Assert.Equal("Implement service", task.Description);
        Assert.False(task.IsComplete);
    }

    [Fact]
    public void FindNextIncompleteTask_AllComplete_ReturnsNull()
    {
        var content = """
            ### Phase 1: Setup

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 1.1 | ✅ Setup project | `project.csproj` | Project builds |
            | 1.2 | ✅ Add config | `appsettings.json` | Config loads |
            """;

        var task = _parser.FindNextIncompleteTask(content, 1);
        Assert.Null(task);
    }

    [Fact]
    public void FindNextIncompleteTask_NoMatchingPhase_ReturnsNull()
    {
        var content = """
            ### Phase 1: Setup

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 1.1 | Setup project | `project.csproj` | Project builds |
            """;

        var task = _parser.FindNextIncompleteTask(content, 5);
        Assert.Null(task);
    }

    [Fact]
    public void FindNextIncompleteTask_EmptyContent_ReturnsNull()
    {
        var task = _parser.FindNextIncompleteTask("", 1);
        Assert.Null(task);
    }

    #endregion

    #region HasIncompleteTasks

    [Fact]
    public void HasIncompleteTasks_WithIncomplete_ReturnsTrue()
    {
        var content = """
            ### Phase 3: Testing

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 3.1 | ✅ Unit tests | `UnitTests.cs` | Tests pass |
            | 3.2 | Integration tests | `IntegrationTests.cs` | Tests pass |
            """;

        Assert.True(_parser.HasIncompleteTasks(content, 3));
    }

    [Fact]
    public void HasIncompleteTasks_AllComplete_ReturnsFalse()
    {
        var content = """
            ### Phase 3: Testing

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 3.1 | ✅ Unit tests | `UnitTests.cs` | Tests pass |
            | 3.2 | ✅ Integration tests | `IntegrationTests.cs` | Tests pass |
            """;

        Assert.False(_parser.HasIncompleteTasks(content, 3));
    }

    [Fact]
    public void HasIncompleteTasks_NoPhase_ReturnsFalse()
    {
        Assert.False(_parser.HasIncompleteTasks("no phases here", 1));
    }

    [Fact]
    public void HasIncompleteTasks_EmptyContent_ReturnsFalse()
    {
        Assert.False(_parser.HasIncompleteTasks("", 1));
    }

    #endregion

    #region Phase Isolation

    [Fact]
    public void ParseTasks_DoesNotLeakTasksFromAdjacentPhases()
    {
        // This test verifies that tasks from adjacent phases in the same document
        // are not incorrectly attributed to the requested phase
        var content = """
            ### Phase 1: First Phase

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 1.1 | Task A | `a.cs` | A works |
            | 1.2 | Task B | `b.cs` | B works |

            ### Phase 2: Second Phase

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 2.1 | Task C | `c.cs` | C works |

            ### Phase 3: Third Phase

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 3.1 | Task D | `d.cs` | D works |
            | 3.2 | Task E | `e.cs` | E works |
            | 3.3 | Task F | `f.cs` | F works |
            """;

        // Verify each phase only returns its own tasks
        var phase1 = _parser.ParseTasks(content, 1);
        Assert.Equal(2, phase1.Count);
        Assert.Equal("1.1", phase1[0].StepId);
        Assert.Equal("1.2", phase1[1].StepId);

        var phase2 = _parser.ParseTasks(content, 2);
        Assert.Single(phase2);
        Assert.Equal("2.1", phase2[0].StepId);

        var phase3 = _parser.ParseTasks(content, 3);
        Assert.Equal(3, phase3.Count);
        Assert.Equal("3.1", phase3[0].StepId);
        Assert.Equal("3.2", phase3[1].StepId);
        Assert.Equal("3.3", phase3[2].StepId);
    }

    [Fact]
    public void FindNextIncompleteTask_OnlyConsidersTasksInRequestedPhase()
    {
        var content = """
            ### Phase 1: First Phase

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 1.1 | ✅ Task A | `a.cs` | A works |
            | 1.2 | ✅ Task B | `b.cs` | B works |

            ### Phase 2: Second Phase

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 2.1 | Task C | `c.cs` | C works |
            """;

        // Phase 1 is all complete
        Assert.Null(_parser.FindNextIncompleteTask(content, 1));

        // Phase 2 has incomplete task
        var task = _parser.FindNextIncompleteTask(content, 2);
        Assert.NotNull(task);
        Assert.Equal("2.1", task.StepId);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ParseTasks_WithHighPhaseNumbers_ParsesCorrectly()
    {
        var content = """
            ### Phase 12: Final Integration

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 12.1 | Integration test suite | `tests/Integration.cs` | All tests pass |
            | 12.2 | Performance benchmarks | `tests/Benchmarks.cs` | Under threshold |
            """;

        var tasks = _parser.ParseTasks(content, 12);

        Assert.Equal(2, tasks.Count);
        Assert.Equal("12.1", tasks[0].StepId);
        Assert.Equal(12, tasks[0].PhaseNumber);
        Assert.Equal(1, tasks[0].TaskNumber);
        Assert.Equal("12.2", tasks[1].StepId);
        Assert.Equal(2, tasks[1].TaskNumber);
    }

    [Fact]
    public void ParseTasks_WithMultipleFilesInColumn_PreservesFullText()
    {
        var content = """
            ### Phase 1: Setup

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 1.1 | Setup both files | `src/A.cs`, `src/B.cs` | Both files created |
            """;

        var tasks = _parser.ParseTasks(content, 1);

        Assert.Single(tasks);
        Assert.Contains("A.cs", tasks[0].Files);
        Assert.Contains("B.cs", tasks[0].Files);
    }

    #endregion
}
