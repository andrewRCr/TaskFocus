# TaskFocus v2.0 Modernization

This document explains the current state of TaskFocus during its transition from v1.0 to v2.0.

## Current Repository State

### TaskFocus v1.0 (Stable Release)

- **Location**: `/legacy` directory
- **Status**: Complete and functional
- **Architecture**: .NET 8 API + Blazor Web App + WPF Desktop App
- **Deployment**: Available at [taskfocus.andrewcreekmore.com](https://taskfocus.andrewcreekmore.com)
- **Downloads**: [GitHub Releases](https://github.com/andrewRCr/TaskFocus/releases/latest)

### TaskFocus v2.0 (In Development)

- **Location**: `/src` directory (currently being built)
- **Status**: Architecture planning and foundation setup phase
- **Target**: Complete modernization focusing on maintainability and portfolio showcase

## What's Changing in v2.0

### Architecture

- **From**: Layered architecture with mixed patterns
- **To**: Clean Architecture (Onion) with Vertical Slice patterns
- **Benefits**: Better separation of concerns, testability, and maintainability

### Desktop Application

- **From**: WPF with Caliburn.Micro (MVVM)
- **To**: .NET MAUI Hybrid (Blazor-powered)
- **Benefits**: Cross-platform capability, shared UI components with web app

### Development Practices

- **Enhanced**: Comprehensive testing (unit, integration, end-to-end)
- **Enhanced**: Automated CI/CD with GitHub Actions
- **Enhanced**: Infrastructure as Code with Terraform
- **Enhanced**: Modern deployment to Azure Container Apps

### Code Quality

- **Enhanced**: Static analysis and linting enforcement
- **Enhanced**: Architectural Decision Records (ADRs)
- **Enhanced**: Comprehensive documentation and development workflows

## Timeline & Status

### ✅ Completed (Phase 0)

- Project restructuring and legacy code preservation
- Architecture planning and technology stack selection
- Development environment setup (ARC framework integration)
- Documentation and workflow establishment

### 🚧 Current (Phase 1 - M1: Foundational Slice)

- Core architectural skeleton implementation
- CI/CD pipeline setup with GitHub Actions
- Basic domain entities and application patterns

### 📋 Upcoming

- M2: Continuous Deployment infrastructure
- M3: Core Task CRUD functionality
- M4: Authentication and user identity
- M5: Organizational features (Projects & Contexts)
- M6: Advanced UI and state management
- M7: Stretch goals (real-time sync, natural language input)

## For Developers

### Exploring the Codebase

- **Legacy v1.0**: Browse `/legacy` for the current implementation
- **Modern v2.0**: Follow progress in `/src` as it develops
- **Documentation**: See `.arc/reference/constitution/` for detailed specifications

### Contributing

v2.0 is currently a solo portfolio project focused on demonstrating modern development practices.
The codebase serves as a showcase of Clean Architecture, comprehensive testing, and professional
development workflows.

## Technical Details

For comprehensive technical information, see:

- [Technical Architecture](../.arc/reference/constitution/TECHNICAL-ARCHITECTURE.md)
- [Meta Product Requirements](../.arc/reference/constitution/META-PRD.md)
- [Project Status](../.arc/reference/constitution/PROJECT-STATUS.md)

---

*This modernization represents a complete rebuild focused on code quality, maintainability, and modern
development practices while preserving the core functionality that makes TaskFocus valuable.*
