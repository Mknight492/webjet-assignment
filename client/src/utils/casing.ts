/**
 * Recursively transforms object keys from PascalCase to camelCase
 */
export function toCamelCase<T>(obj: any): T {
  if (obj === null || obj === undefined || typeof obj !== 'object') {
    return obj;
  }

  if (Array.isArray(obj)) {
    return obj.map(toCamelCase) as unknown as T;
  }

  return Object.keys(obj).reduce((acc, key) => {
    // Convert the first character to lowercase for camelCase
    const camelKey = key.charAt(0).toLowerCase() + key.slice(1);
    acc[camelKey] = toCamelCase(obj[key]);
    return acc;
  }, {} as any) as T;
} 