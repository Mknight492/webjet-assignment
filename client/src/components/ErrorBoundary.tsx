import React, { Component, ErrorInfo, ReactNode } from 'react';

interface Props {
  children: ReactNode;
  fallback?: ReactNode;
}

interface State {
  hasError: boolean;
}

class ErrorBoundary extends Component<Props, State> {
  public state: State = {
    hasError: false
  };

  public static getDerivedStateFromError(_: Error): State {
    return { hasError: true };
  }

  public componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error("Uncaught error:", error, errorInfo);
  }

  public render() {
    if (this.state.hasError) {
      return this.props.fallback || (
        <div className="p-4 border border-red-200 bg-red-50 rounded-md">
          <h2 className="text-red-700 font-medium">Something went wrong</h2>
          <p className="text-red-600 text-sm">
            The content could not be displayed. Please try again later.
          </p>
        </div>
      );
    }

    return this.props.children;
  }
}

export default ErrorBoundary; 