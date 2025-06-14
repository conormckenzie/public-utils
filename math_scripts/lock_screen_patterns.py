#!/usr/bin/env python3
import itertools

# Define the 3x3 grid for easier visualization (not directly used in logic, but helpful)
# 1 2 3
# 4 5 6
# 7 8 9

# Pre-calculate intermediate points for all possible straight line segments.
# The keys are sorted tuples (smaller_digit, larger_digit) to handle both directions easily.
# Value is the intermediate digit.
PASS_THROUGH_MAP = {
    (1, 3): 2,
    (4, 6): 5,
    (7, 9): 8,
    (1, 7): 4,
    (2, 8): 5,
    (3, 9): 6,
    (1, 9): 5, # Diagonal
    (3, 7): 5  # Diagonal
}

def is_valid_pattern(pattern_str):
    """
    Checks if a given pattern string (e.g., "123", "537821964") represents a valid
    Android lock screen pattern based on the specified rules.
    """
    
    # Constraint 1: Length Constraint
    # Pattern must contain at least 4 digits and at most 9 digits.
    if not (4 <= len(pattern_str) <= 9):
        return False

    # Constraint 2: Digit Validity and Uniqueness Constraint
    # All digits must be 1-9 and unique.
    # This is implicitly handled by itertools.permutations(range(1, 10), length)
    # used in the generation loop, so no explicit check is needed here.
    # If the input `pattern_str` could be arbitrary, you'd add:
    # if not all('1' <= c <= '9' for c in pattern_str): return False
    # if len(set(pattern_str)) != len(pattern_str): return False

    # Constraint 3: Pass-Through Constraint
    # If a line segment (prev_node -> current_node) passes through an intermediate node,
    # that intermediate node MUST have already been visited earlier in the sequence.
    
    # path_so_far stores the nodes visited *before* the current segment's target node.
    path_so_far = set() 

    for i in range(len(pattern_str)):
        current_node = int(pattern_str[i])

        # For any node after the first one, check the segment leading to it.
        if i > 0:
            prev_node = int(pattern_str[i-1])
            
            # Determine if there's an intermediate node for the (prev_node -> current_node) segment.
            # Use tuple(sorted(...)) to get a consistent key for the map regardless of direction.
            intermediate = PASS_THROUGH_MAP.get(tuple(sorted((prev_node, current_node))))

            if intermediate: # If an intermediate node exists for this segment
                # According to the rule, this intermediate node *must* have already been visited.
                # If it's not in `path_so_far`, it means the sequence `pattern_str` skipped it.
                # Such a sequence cannot be formed directly on the grid because the system would
                # automatically activate the intermediate node.
                if intermediate not in path_so_far:
                    return False # This pattern string is invalid because it implies skipping an intermediate
        
        # Add the current_node to the set of visited nodes for subsequent checks.
        path_so_far.add(current_node)

    return True # All constraints satisfied

def generate_and_validate_patterns():
    """
    Generates all possible digit sequences (numbers) and validates them
    against the Android lock pattern rules.
    """
    valid_patterns = []
    all_digits = range(1, 10) # Digits 1 through 9

    # Iterate through possible pattern lengths (from 4 to 9)
    for length in range(4, 10):
        # itertools.permutations generates unique sequences of 'length' digits
        # from 'all_digits'. This intrinsically satisfies the uniqueness constraint.
        for perm in itertools.permutations(all_digits, length):
            pattern_str = "".join(map(str, perm)) # Convert the tuple of digits to a string
            if is_valid_pattern(pattern_str):
                valid_patterns.append(pattern_str)
                
    return valid_patterns

if __name__ == "__main__":
    found_patterns = generate_and_validate_patterns()
    
    for pattern in found_patterns:
        print(pattern)
