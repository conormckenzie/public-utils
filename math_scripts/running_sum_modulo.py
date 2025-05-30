#!/usr/bin/env python3
import sys
import argparse

def main():
    parser = argparse.ArgumentParser(
        description="Calculates the running sum of numbers from stdin, modulo M. "
                    "Reads numbers one per line from stdin. "
                    "Outputs the running sum modulo M for each input number to stdout. "
                    "Use --verbose for additional information on stderr."
    )
    parser.add_argument(
        "M",
        type=int,
        help="The modulus M."
    )
    parser.add_argument(
        "--verbose",
        action="store_true",
        help="Print informational messages to stderr."
    )

    args = parser.parse_args()
    modulus = args.M

    if modulus <= 0:
        if args.verbose:
            print(f"Error: Modulus M must be a positive integer. Got {modulus}", file=sys.stderr)
        sys.exit(1)

    if args.verbose:
        print(f"Calculating running sum modulo {modulus}. Reading numbers from stdin...", file=sys.stderr)

    running_sum = 0
    line_number = 0
    for line in sys.stdin:
        line_number += 1
        try:
            number = int(line.strip())
            running_sum += number
            result = running_sum % modulus
            print(result, flush=True) # Output to stdout
            if args.verbose:
                print(f"Input: {number}, Running Sum: {running_sum}, Sum % {modulus}: {result}", file=sys.stderr)
        except ValueError:
            if args.verbose:
                print(f"Warning: Skipping non-integer input on line {line_number}: '{line.strip()}'", file=sys.stderr)
            # Continue processing other lines

    if args.verbose:
        print("Finished processing stdin.", file=sys.stderr)

if __name__ == "__main__":
    main()
