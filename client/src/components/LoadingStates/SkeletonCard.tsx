import React from "react";

const SkeletonCard: React.FC = () => {
  return (
    <div className="bg-white rounded-lg shadow-md overflow-hidden animate-pulse">
      <div className="w-full pb-[150%] bg-gray-200 relative"></div>
      <div className="p-4">
        <div className="h-5 bg-gray-200 rounded w-3/4 mb-2"></div>
        <div className="h-4 bg-gray-200 rounded w-1/2 mb-4"></div>
        
        <div className="space-y-2 mt-4">
          <div className="flex justify-between">
            <div className="h-4 bg-gray-200 rounded w-1/3"></div>
            <div className="h-4 bg-gray-200 rounded w-1/4"></div>
          </div>
          <div className="flex justify-between">
            <div className="h-4 bg-gray-200 rounded w-1/3"></div>
            <div className="h-4 bg-gray-200 rounded w-1/4"></div>
          </div>
        </div>
        
        <div className="mt-3 flex justify-center">
          <div className="h-5 bg-gray-200 rounded w-1/2"></div>
        </div>
      </div>
    </div>
  );
};

export default SkeletonCard; 