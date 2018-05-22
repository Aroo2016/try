﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Clockwise;
using Microsoft.AspNetCore.Mvc;
using Pocket;
using WorkspaceServer;
using WorkspaceServer.Models;
using WorkspaceServer.Servers.Dotnet;
using WorkspaceServer.Servers.Scripting;
using static Pocket.Logger<MLS.Agent.Controllers.LanguageServicesController>;
using Workspace = WorkspaceServer.Models.Execution.Workspace;

namespace MLS.Agent.Controllers
{
    public class LanguageServicesController : WorkspaceServerController
    {
        public LanguageServicesController(WorkspaceServerRegistry workspaceServerRegistry) : base(workspaceServerRegistry)
        {
        }

        [HttpPost]
        [Route("/workspace/completion")]
        public async Task<IActionResult> Completion(
            [FromBody] WorkspaceRequest request,
            [FromHeader(Name = "Timeout")] string timeoutInMilliseconds = "15000")
        {
            if (Debugger.IsAttached && !(Clock.Current is VirtualClock))
            {
                AddToDisposeChain(VirtualClock.Start());
            }

            using (var operation = Log.OnEnterAndConfirmOnExit())
            {
                operation.Info("Processing workspaceType {workspaceType}", request.Workspace.WorkspaceType);
                if (!int.TryParse(timeoutInMilliseconds, out var timeoutMs))
                {
                    return BadRequest();
                }

                var runTimeout = TimeSpan.FromMilliseconds(timeoutMs);
                var budget = new TimeBudget(runTimeout);
                var server = await GetServerForWorkspace(request.Workspace, budget);
                var result = await server.GetCompletionList(request, budget);
                budget.RecordEntry();
                operation.Succeed();

                return Ok(result);
            }
        }

        [HttpPost]
        [Route("/workspace/diagnostics")]
        public async Task<IActionResult> Diagnostics(
            [FromBody] Workspace request,
            [FromHeader(Name = "Timeout")] string timeoutInMilliseconds = "15000")
        {
            if (Debugger.IsAttached && !(Clock.Current is VirtualClock))
            {
                AddToDisposeChain(VirtualClock.Start());
            }

            using (var operation = Log.OnEnterAndConfirmOnExit())
            {
                if (!int.TryParse(timeoutInMilliseconds, out var timeoutMs))
                {
                    return BadRequest();
                }

                var runTimeout = TimeSpan.FromMilliseconds(timeoutMs);
                var budget = new TimeBudget(runTimeout);
                var server = await GetServerForWorkspace(request, budget);
                var result = await server.GetDiagnostics(request, budget);
                budget.RecordEntry();
                operation.Succeed();

                return Ok(result);
            }
        }

        [HttpPost]
        [Route("/workspace/signaturehelp")]
        public async Task<IActionResult> SignatureHelp(
            [FromBody] WorkspaceRequest request,
            [FromHeader(Name = "Timeout")] string timeoutInMilliseconds = "15000")
        {
            if (Debugger.IsAttached && !(Clock.Current is VirtualClock))
            {
                AddToDisposeChain(VirtualClock.Start());
            }

            using (var operation = Log.OnEnterAndConfirmOnExit())
            {
                operation.Info("Processing workspaceType {workspaceType}", request.Workspace.WorkspaceType);
                if (!int.TryParse(timeoutInMilliseconds, out var timeoutMs))
                {
                    return BadRequest();
                }

                var runTimeout = TimeSpan.FromMilliseconds(timeoutMs);
                var budget = new TimeBudget(runTimeout);
                var server = await GetServerForWorkspace(request.Workspace, budget);
                var result = await server.GetSignatureHelp(request, budget);
                budget.RecordEntry();
                operation.Succeed();

                return Ok(result);
            }
        }

        private async Task<IWorkspaceServer> GetServerForWorkspace(Workspace workspace, Budget budget)
        {
            budget?.RecordEntryAndThrowIfBudgetExceeded();
            IWorkspaceServer server;
            var workspaceType = workspace.WorkspaceType;
            using (var operation = Log.OnEnterAndConfirmOnExit())
            {
                if (string.Equals(workspaceType, "script", StringComparison.OrdinalIgnoreCase))
                {
                    server = new ScriptingWorkspaceServer();
                }
                else
                {
                    server = await GetWorkspaceServer(workspaceType, budget);

                    if (server is DotnetWorkspaceServer dotnetWorkspaceServer)
                    {
                        await dotnetWorkspaceServer.EnsureInitializedAndNotDisposed(budget);
                    }
                }

                budget?.RecordEntry();
                operation.Succeed();
            }

            return server;
        }
    }
}
