using Hugh.Detection;

namespace Hugh.Tests;

/// <summary>
/// Unit tests for PlanCreationPhaseParser.
/// Tests parsing of plan skeleton phase headers, task table detection,
/// and incomplete phase discovery.
/// </summary>
public class PlanCreationPhaseParserTests
{
    private readonly PlanCreationPhaseParser _parser = new();

    #region ParsePhases

    [Fact]
    public void ParsePhases_WithSkeletonDocument_ReturnsAllPhases()
    {
        var content = """
            # Implementation Plan

            ## Execution Plan

            ### Phase 1: Setup & Foundation
            **Status:** ⏳ Not Started
            **Size:** Small
            **Prerequisites:** None

            ### Phase 2: Core Logic
            **Status:** ⏳ Not Started
            **Size:** Medium
            **Prerequisites:** Phase 1

            ### Phase 3: Integration & Testing
            **Status:** ⏳ Not Started
            **Size:** Large
            **Prerequisites:** Phase 2
            """;

        var phases = _parser.ParsePhases(content);

        Assert.Equal(3, phases.Count);
        Assert.Equal(1, phases[0].Number);
        Assert.Equal("Setup & Foundation", phases[0].Name);
        Assert.False(phases[0].HasTaskTable);

        Assert.Equal(2, phases[1].Number);
        Assert.Equal("Core Logic", phases[1].Name);
        Assert.False(phases[1].HasTaskTable);

        Assert.Equal(3, phases[2].Number);
        Assert.Equal("Integration & Testing", phases[2].Name);
        Assert.False(phases[2].HasTaskTable);
    }

    [Fact]
    public void ParsePhases_DetectsCompletedPhaseWithTaskTable()
    {
        var content = """
            ## Execution Plan

            ### Phase 1: Setup
            **Status:** ✅ Complete

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 1.1 | Create config file | `config.json` | File exists |
            | 1.2 | Add dependencies | `package.json` | Deps installed |

            ### Phase 2: Implementation
            **Status:** ⏳ Not Started
            **Size:** Medium
            """;

        var phases = _parser.ParsePhases(content);

        Assert.Equal(2, phases.Count);
        Assert.True(phases[0].HasTaskTable);
        Assert.False(phases[1].HasTaskTable);
    }

    [Fact]
    public void ParsePhases_EmptyContent_ReturnsEmpty()
    {
        Assert.Empty(_parser.ParsePhases(""));
        Assert.Empty(_parser.ParsePhases("   "));
        Assert.Empty(_parser.ParsePhases(null!));
    }

    [Fact]
    public void ParsePhases_NoPhaseHeaders_ReturnsEmpty()
    {
        var content = """
            # Some Document
            ## Section One
            Some text here.
            ## Section Two
            More text.
            """;

        Assert.Empty(_parser.ParsePhases(content));
    }

    [Fact]
    public void ParsePhases_ExtractsScopeText()
    {
        var content = """
            ### Phase 1: Setup
            **Status:** ⏳ Not Started
            **Size:** Small
            **Entry Point:** `src/config.ts`

            ### Phase 2: Core
            **Status:** ⏳ Not Started
            """;

        var phases = _parser.ParsePhases(content);

        Assert.Equal(2, phases.Count);
        Assert.Contains("Entry Point:", phases[0].Scope);
        Assert.Contains("Small", phases[0].Scope);
    }

    [Fact]
    public void ParsePhases_MixedCompletionStates()
    {
        var content = """
            ### Phase 1: Foundation
            **Status:** ✅ Complete

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 1.1 | Init project | `init.sh` | Project initialized |

            ### Phase 2: Core Logic
            **Status:** ✅ Complete

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 2.1 | Add models | `models.cs` | Models compile |

            ### Phase 3: API Layer
            **Status:** ⏳ Not Started
            **Size:** Medium

            ### Phase 4: Testing
            **Status:** ⏳ Not Started
            **Size:** Large
            """;

        var phases = _parser.ParsePhases(content);

        Assert.Equal(4, phases.Count);
        Assert.True(phases[0].HasTaskTable);
        Assert.True(phases[1].HasTaskTable);
        Assert.False(phases[2].HasTaskTable);
        Assert.False(phases[3].HasTaskTable);
    }

    [Fact]
    public void ParsePhases_AllPhasesHaveTaskTables()
    {
        var content = """
            ### Phase 1: Setup

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 1.1 | Configure | `config.json` | Config valid |

            ### Phase 2: Build

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 2.1 | Compile | `src/main.cs` | Compiles |
            """;

        var phases = _parser.ParsePhases(content);

        Assert.Equal(2, phases.Count);
        Assert.True(phases[0].HasTaskTable);
        Assert.True(phases[1].HasTaskTable);
    }

    [Fact]
    public void ParsePhases_WithDashSeparator_ReturnsPhases()
    {
        var content = """
            ## Execution Plan

            ### Phase 1 - Setup & Foundation
            **Status:** ⏳ Not Started
            **Size:** Small

            ### Phase 2 — Core Logic
            **Status:** ⏳ Not Started
            **Size:** Medium

            ### Phase 3 – Integration & Testing
            **Status:** ⏳ Not Started
            **Size:** Large
            """;

        var phases = _parser.ParsePhases(content);

        Assert.Equal(3, phases.Count);
        Assert.Equal(1, phases[0].Number);
        Assert.Equal("Setup & Foundation", phases[0].Name);
        Assert.Equal(2, phases[1].Number);
        Assert.Equal("Core Logic", phases[1].Name);
        Assert.Equal(3, phases[2].Number);
        Assert.Equal("Integration & Testing", phases[2].Name);
    }

    #endregion

    #region FindNextIncompletePhase

    [Fact]
    public void FindNextIncompletePhase_SkeletonWithEmptyPhases_ReturnsFirst()
    {
        var content = """
            ### Phase 1: Setup
            **Status:** ⏳ Not Started

            ### Phase 2: Core
            **Status:** ⏳ Not Started

            ### Phase 3: Testing
            **Status:** ⏳ Not Started
            """;

        var next = _parser.FindNextIncompletePhase(content);

        Assert.NotNull(next);
        Assert.Equal(1, next.Number);
        Assert.Equal("Setup", next.Name);
        Assert.False(next.HasTaskTable);
    }

    [Fact]
    public void FindNextIncompletePhase_FirstPhaseComplete_ReturnsSecond()
    {
        var content = """
            ### Phase 1: Setup

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 1.1 | Init | `init.sh` | Project inited |

            ### Phase 2: Core Logic
            **Status:** ⏳ Not Started

            ### Phase 3: Testing
            **Status:** ⏳ Not Started
            """;

        var next = _parser.FindNextIncompletePhase(content);

        Assert.NotNull(next);
        Assert.Equal(2, next.Number);
        Assert.Equal("Core Logic", next.Name);
    }

    [Fact]
    public void FindNextIncompletePhase_AllComplete_ReturnsNull()
    {
        var content = """
            ### Phase 1: Setup

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 1.1 | Init | `init.sh` | Project inited |

            ### Phase 2: Core

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 2.1 | Build | `build.sh` | Builds OK |
            """;

        var next = _parser.FindNextIncompletePhase(content);

        Assert.Null(next);
    }

    [Fact]
    public void FindNextIncompletePhase_MiddlePhaseIncomplete_ReturnsThatPhase()
    {
        var content = """
            ### Phase 1: Setup

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 1.1 | Init | `init.sh` | Project inited |

            ### Phase 2: Core Logic
            **Status:** ⏳ Not Started

            ### Phase 3: Testing

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 3.1 | Tests | `test.sh` | Tests pass |
            """;

        var next = _parser.FindNextIncompletePhase(content);

        Assert.NotNull(next);
        Assert.Equal(2, next.Number);
        Assert.Equal("Core Logic", next.Name);
    }

    #endregion

    #region HasIncompletePhases

    [Fact]
    public void HasIncompletePhases_WithEmptyPhases_ReturnsTrue()
    {
        var content = """
            ### Phase 1: Setup

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 1.1 | Init | `init.sh` | Done |

            ### Phase 2: Core
            **Status:** ⏳ Not Started
            """;

        Assert.True(_parser.HasIncompletePhases(content));
    }

    [Fact]
    public void HasIncompletePhases_AllComplete_ReturnsFalse()
    {
        var content = """
            ### Phase 1: Setup

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 1.1 | Init | `init.sh` | Done |

            ### Phase 2: Core

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 2.1 | Build | `build.sh` | Done |
            """;

        Assert.False(_parser.HasIncompletePhases(content));
    }

    [Fact]
    public void HasIncompletePhases_EmptyContent_ReturnsFalse()
    {
        Assert.False(_parser.HasIncompletePhases(""));
        Assert.False(_parser.HasIncompletePhases("   "));
    }

    [Fact]
    public void HasIncompletePhases_AllPhasesEmpty_ReturnsTrue()
    {
        var content = """
            ### Phase 1: First
            **Status:** ⏳ Not Started

            ### Phase 2: Second
            **Status:** ⏳ Not Started
            """;

        Assert.True(_parser.HasIncompletePhases(content));
    }

    #endregion

    #region GetIncompletePhaseNumbers

    [Fact]
    public void GetIncompletePhaseNumbers_MixedStates_ReturnsIncomplete()
    {
        var content = """
            ### Phase 1: Setup

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 1.1 | Init | `init.sh` | Done |

            ### Phase 2: Core
            **Status:** ⏳ Not Started

            ### Phase 3: API

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 3.1 | Endpoints | `api.cs` | Done |

            ### Phase 4: Testing
            **Status:** ⏳ Not Started

            ### Phase 5: Deployment
            **Status:** ⏳ Not Started
            """;

        var incomplete = _parser.GetIncompletePhaseNumbers(content);

        Assert.Equal(3, incomplete.Count);
        Assert.Contains(2, incomplete);
        Assert.Contains(4, incomplete);
        Assert.Contains(5, incomplete);
    }

    [Fact]
    public void GetIncompletePhaseNumbers_AllComplete_ReturnsEmpty()
    {
        var content = """
            ### Phase 1: Setup

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 1.1 | Setup | `s.sh` | Done |

            ### Phase 2: Build

            | Step | Task | Files | Acceptance Criteria |
            |------|------|-------|---------------------|
            | 2.1 | Build | `b.sh` | Done |
            """;

        Assert.Empty(_parser.GetIncompletePhaseNumbers(content));
    }

    [Fact]
    public void GetIncompletePhaseNumbers_EmptyContent_ReturnsEmpty()
    {
        Assert.Empty(_parser.GetIncompletePhaseNumbers(""));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ParsePhases_PhaseHeaderWithoutColon_NotMatched()
    {
        // The regex requires "### Phase N: Name" format
        var content = """
            ### Phase 1 Setup Without Colon
            Some text

            ### Phase 2: Proper Format
            Some text
            """;

        var phases = _parser.ParsePhases(content);

        Assert.Single(phases);
        Assert.Equal(2, phases[0].Number);
    }

    [Fact]
    public void ParsePhases_TaskTableWithDifferentColumnOrder_StillDetected()
    {
        // The regex checks for both "Step" and "Task" columns in any order
        var content = """
            ### Phase 1: Example

            | Step | Files | Task | Notes |
            |------|-------|------|-------|
            | 1.1 | `file.cs` | Do thing | note |
            """;

        var phases = _parser.ParsePhases(content);

        Assert.Single(phases);
        Assert.True(phases[0].HasTaskTable);
    }

    [Fact]
    public void ParsePhases_PhaseAtEndOfDocument()
    {
        var content = """
            ### Phase 1: First

            | Step | Task | Files | AC |
            |------|------|-------|----|
            | 1.1 | Init | `x.cs` | OK |

            ### Phase 2: Last Phase At End
            **Status:** ⏳ Not Started
            **Size:** Small
            """;

        var phases = _parser.ParsePhases(content);

        Assert.Equal(2, phases.Count);
        Assert.True(phases[0].HasTaskTable);
        Assert.False(phases[1].HasTaskTable);
        Assert.Equal("Last Phase At End", phases[1].Name);
    }

    #endregion
}
