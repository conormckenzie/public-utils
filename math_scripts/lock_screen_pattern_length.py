#!/usr/bin/env python3
import sys
import math

# Define the (x, y) coordinates for each digit on a 3x3 grid
# Assuming a grid where (0,0) is top-left, (2,2) is bottom-right
NODE_COORDINATES = {
    1: (0, 0), 2: (1, 0), 3: (2, 0),
    4: (0, 1), 5: (1, 1), 6: (2, 1),
    7: (0, 2), 8: (1, 2), 9: (2, 2)
}

class ExactLength:
    """
    Represents an exact length as a sum of coefficients multiplied by square roots.
    e.g., C0 + C1*sqrt(2) + C2*sqrt(5) + C3*sqrt(8)
    """
    def __init__(self, c0=0, c_sqrt2=0, c_sqrt5=0, c_sqrt8=0):
        self.c0 = c0
        self.c_sqrt2 = c_sqrt2
        self.c_sqrt5 = c_sqrt5
        self.c_sqrt8 = c_sqrt8

    def __add__(self, other):
        if not isinstance(other, ExactLength):
            raise TypeError("Can only add ExactLength objects")
        return ExactLength(
            self.c0 + other.c0,
            self.c_sqrt2 + other.c_sqrt2,
            self.c_sqrt5 + other.c_sqrt5,
            self.c_sqrt8 + other.c_sqrt8
        )

    def __str__(self):
        # Format each component with a fixed width for coefficients
        # and always include all terms for constant width.
        # Using a format like "C0 + C_sqrt2*sqrt(2) + C_sqrt5*sqrt(5) + C_sqrt8*sqrt(8)"
        # We can use f-strings with alignment for fixed width.
        # Let's assume coefficients won't exceed 3 digits for now, adjust if needed.
        # Example: " 5 +  2*sqrt(2) +  0*sqrt(5) +  1*sqrt(8)"
        # Use a compact representation without spaces to avoid issues with `sort`
        # Example: "C0+C_sqrt2*sqrt(2)+C_sqrt5*sqrt(5)+C_sqrt8*sqrt(8)"
        # To ensure constant width for column 2, we need to pad each coefficient.
        # Max coefficient value is 8 (for a 9-node path, all segments of one type).
        # So, 2 digits for padding should be sufficient (e.g., " 8").
        # The format will be "C0+C_sqrt2*sqrt(2)+C_sqrt5*sqrt(5)+C_sqrt8*sqrt(8)"
        # with C0, C_sqrt2, C_sqrt5, C_sqrt8 padded.
        # To ensure constant width and no internal spaces for `sort`,
        # we will use zero-padded coefficients and a consistent separator.
        # Format: "C0_C_sqrt2_C_sqrt5_C_sqrt8"
        # Example: "05_02_00_01"
        return (f"{self.c0:02}_"
                f"{self.c_sqrt2:02}s2_" # 's2' for sqrt(2)
                f"{self.c_sqrt5:02}s5_" # 's5' for sqrt(5)
                f"{self.c_sqrt8:02}s8") # 's8' for sqrt(8)

    def approximate(self):
        return (self.c0 + 
                self.c_sqrt2 * math.sqrt(2) + 
                self.c_sqrt5 * math.sqrt(5) + 
                self.c_sqrt8 * math.sqrt(8))

def calculate_segment_length(node1: int, node2: int) -> ExactLength:
    """
    Calculates the exact Euclidean length between two nodes on the grid.
    Returns an ExactLength object.
    """
    x1, y1 = NODE_COORDINATES[node1]
    x2, y2 = NODE_COORDINATES[node2]

    dx = abs(x1 - x2)
    dy = abs(y1 - y2)

    # Calculate squared distance
    dist_sq = dx**2 + dy**2

    if dist_sq == 1: # e.g., 1-2, 4-5 (dx=1, dy=0 or dx=0, dy=1)
        return ExactLength(c0=1)
    elif dist_sq == 2: # e.g., 1-5, 2-4 (dx=1, dy=1)
        return ExactLength(c_sqrt2=1)
    elif dist_sq == 4: # e.g., 1-3, 2-8 (dx=2, dy=0 or dx=0, dy=2)
        return ExactLength(c0=2) # sqrt(4) = 2
    elif dist_sq == 5: # e.g., 1-6, 2-7 (dx=2, dy=1 or dx=1, dy=2)
        return ExactLength(c_sqrt5=1)
    elif dist_sq == 8: # e.g., 1-9, 3-7 (dx=2, dy=2)
        return ExactLength(c_sqrt8=1) # sqrt(8) = 2*sqrt(2)
    else:
        # This case should ideally not be reached for valid lock patterns
        # as all segments will fall into one of the above categories.
        # However, for robustness, we can return a generic sqrt.
        # For this problem, we only care about sqrt(2), sqrt(5), sqrt(8)
        # and integer lengths.
        raise ValueError(f"Unexpected squared distance: {dist_sq} for segment {node1}-{node2}")

def calculate_pattern_length(pattern_str: str) -> tuple[ExactLength, float]:
    """
    Calculates the total exact and approximate length of a lock screen pattern.
    """
    total_exact_length = ExactLength()
    
    for i in range(len(pattern_str) - 1):
        node1 = int(pattern_str[i])
        node2 = int(pattern_str[i+1])
        segment_length = calculate_segment_length(node1, node2)
        total_exact_length += segment_length
        
    return total_exact_length, total_exact_length.approximate()

if __name__ == "__main__":
    for line in sys.stdin:
        pattern = line.strip()
        if pattern:
            exact_len, approx_len = calculate_pattern_length(pattern)
            # Pad pattern to a fixed width (max 9 digits) for consistent column alignment
            print(f"{pattern:<9}\t{exact_len}\t{approx_len:.6f}")
