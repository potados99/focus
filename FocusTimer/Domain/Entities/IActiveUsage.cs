﻿// IActiveUsage.cs
// 이 파일은 FocusTimer의 일부입니다.
// 
// © 2023 Potados <song@potados.com>
// 
// FocusTimer은(는) 자유 소프트웨어입니다.
// GNU General Public License v3.0을 준수하는 범위 내에서
// 누구든지 자유롭게 변경, 수정하거나 배포할 수 있습니다.
// 
// 이 프로그램을 공유함으로서 다른 누군가에게 도움이 되기를 바랍니다.
// 다만 프로그램 배포와 함께 아무 것도 보증하지 않습니다. 자세한 내용은
// GNU General Public License를 참고하세요.
// 
// 라이센스 전문은 이 프로그램과 함께 제공되었을 것입니다. 만약 아니라면,
// 다음 링크에서 받아볼 수 있습니다: <https://www.gnu.org/licenses/gpl-3.0.txt>

namespace FocusTimer.Domain.Entities;

/// <summary>
/// 타이머에 등록된 앱이 포커스를 얻은 동안에 유지되는 엔티티의 필수 속성을 정의합니다.
/// </summary>
/// <typeparam name="TRunningUsage"></typeparam>
public interface IActiveUsage<TRunningUsage> : IElapsable
    where TRunningUsage : IElapsable
{
    /// <summary>
    /// 이 엔티티를 가지고 있는 부모 격의 RunningUsage입니다.
    /// </summary>
    public TRunningUsage ParentRunningUsage { get; set; }
}